﻿using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;

namespace PathfinderJson.SearchPanel
{
	/// <summary>
	/// TextAreaInputHandler that registers all search-related commands.
	/// </summary>
	public class SearchInputHandler : TextAreaInputHandler
	{
		internal SearchInputHandler(TextArea textArea, SearchPanel panel)
			: base(textArea)
		{
			RegisterCommands(this.CommandBindings);
			this.panel = panel;
		}

		internal void RegisterGlobalCommands(CommandBindingCollection commandBindings)
		{
			commandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
			commandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
			commandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
		}

		void RegisterCommands(ICollection<CommandBinding> commandBindings)
		{
			commandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
			commandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
			commandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
			commandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, ExecuteCloseSearchPanel, CanExecuteWithOpenSearchPanel));
		}

		SearchPanel panel;

		void ExecuteFind(object sender, ExecutedRoutedEventArgs e)
		{
			panel.Open();
			if (!(TextArea.Selection.IsEmpty || TextArea.Selection.IsMultiline))
				panel.SearchPattern = TextArea.Selection.GetText();
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate { panel.Reactivate(); });
		}

		void CanExecuteWithOpenSearchPanel(object sender, CanExecuteRoutedEventArgs e)
		{
			if (panel.IsClosed)
			{
				e.CanExecute = false;
				// Continue routing so that the key gesture can be consumed by another component.
				e.ContinueRouting = true;
			}
			else
			{
				e.CanExecute = true;
				e.Handled = true;
			}
		}

		void ExecuteFindNext(object sender, ExecutedRoutedEventArgs e)
		{
			if (!panel.IsClosed)
			{
				panel.FindNext();
				e.Handled = true;
			}
		}

		void ExecuteFindPrevious(object sender, ExecutedRoutedEventArgs e)
		{
			if (!panel.IsClosed)
			{
				panel.FindPrevious();
				e.Handled = true;
			}
		}

		void ExecuteCloseSearchPanel(object sender, ExecutedRoutedEventArgs e)
		{
			if (!panel.IsClosed)
			{
				panel.Close();
				e.Handled = true;
			}
		}

		/// <summary>
		/// Fired when SearchOptions are modified inside the SearchPanel.
		/// </summary>
		public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged
		{
			add { panel.SearchOptionsChanged += value; }
			remove { panel.SearchOptionsChanged -= value; }
		}
	}
}
