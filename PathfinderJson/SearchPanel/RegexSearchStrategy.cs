using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace PathfinderJson.SearchPanel
{
	/// <summary>
	/// Provides factory methods for ISearchStrategies.
	/// </summary>
	public static class SearchStrategyFactory
	{
		/// <summary>
		/// Creates a default ISearchStrategy with the given parameters.
		/// </summary>
		public static ISearchStrategy Create(string searchPattern, bool ignoreCase, bool matchWholeWords, SearchMode mode)
		{
			if (searchPattern == null)
				throw new ArgumentNullException("searchPattern");
			RegexOptions options = RegexOptions.Compiled | RegexOptions.Multiline;
			if (ignoreCase)
				options |= RegexOptions.IgnoreCase;

			switch (mode)
			{
				case SearchMode.Normal:
					searchPattern = Regex.Escape(searchPattern);
					break;
				case SearchMode.Wildcard:
					searchPattern = ConvertWildcardsToRegex(searchPattern);
					break;
			}
			try
			{
				Regex pattern = new Regex(searchPattern, options);
				return new RegexSearchStrategy(pattern, matchWholeWords);
			}
			catch (ArgumentException ex)
			{
				throw new SearchPatternException(ex.Message, ex);
			}
		}

		static string ConvertWildcardsToRegex(string searchPattern)
		{
			if (string.IsNullOrEmpty(searchPattern))
				return "";

			StringBuilder builder = new StringBuilder();

			foreach (char ch in searchPattern)
			{
				switch (ch)
				{
					case '?':
						builder.Append(".");
						break;
					case '*':
						builder.Append(".*");
						break;
					default:
						builder.Append(Regex.Escape(ch.ToString()));
						break;
				}
			}

			return builder.ToString();
		}
	}

	class RegexSearchStrategy : ISearchStrategy
	{
		readonly Regex searchPattern;
		readonly bool matchWholeWords;

		public RegexSearchStrategy(Regex searchPattern, bool matchWholeWords)
		{
			if (searchPattern == null)
				throw new ArgumentNullException("searchPattern");
			this.searchPattern = searchPattern;
			this.matchWholeWords = matchWholeWords;
		}

		public IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length)
		{
			int endOffset = offset + length;
			foreach (Match? result in searchPattern.Matches(document.Text))
			{
				if (result != null)
				{
					int resultEndOffset = result.Length + result.Index;
					if (offset > result.Index || endOffset < resultEndOffset)
						continue;
					if (matchWholeWords && (!IsWordBorder(document, result.Index) || !IsWordBorder(document, resultEndOffset)))
						continue;
					yield return new SearchResult { StartOffset = result.Index, Length = result.Length, Data = result };
				}
			}
		}

		static bool IsWordBorder(ITextSource document, int offset)
		{
			return TextUtilities.GetNextCaretPosition(document, offset - 1, LogicalDirection.Forward, CaretPositioningMode.WordBorder) == offset;
		}

		public ISearchResult FindNext(ITextSource document, int offset, int length)
		{
			return FindAll(document, offset, length).FirstOrDefault();
		}

		public bool Equals(ISearchStrategy? other)
		{
			if (other is RegexSearchStrategy strategy)
            {
				return strategy != null &&
	strategy.searchPattern.ToString() == searchPattern.ToString() &&
	strategy.searchPattern.Options == searchPattern.Options &&
	strategy.searchPattern.RightToLeft == searchPattern.RightToLeft;
			}
			else
            {
				return false;
            }
		}
	}

	class SearchResult : TextSegment, ISearchResult
	{
		public Match? Data { get; set; }

		public string ReplaceWith(string replacement)
		{
			if (Data == null) return "";
			return Data.Result(replacement);
		}
	}
}
