﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using SolidShineUi;
using Localization = ICSharpCode.AvalonEdit.Search.Localization;

namespace PathfinderJson.SearchPanel
{
    /// <summary>
    /// Provides search functionality for AvalonEdit. It is displayed in the top-right corner of the TextArea.
    /// </summary>
    /// <remarks>
    /// This is modified from the original control created for SharpDevelop/AvalonEdit, to add on support for theming via Solid Shine UI.
    /// </remarks>
    public class SearchPanel : Control
    {
        TextArea? textArea;
        SearchInputHandler? handler;
        TextDocument? currentDocument;
        SearchResultBackgroundRenderer renderer = new SearchResultBackgroundRenderer();
        TextBox? searchTextBox;
        //Popup dropdownPopup;
        SearchPanelAdorner? adorner;

        #region DependencyProperties
        /// <summary>
        /// Dependency property for <see cref="UseRegex"/>.
        /// </summary>
        public static readonly DependencyProperty UseRegexProperty =
            DependencyProperty.Register("UseRegex", typeof(bool), typeof(SearchPanel),
                                        new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should be interpreted as regular expression.
        /// </summary>
        public bool UseRegex
        {
            get { return (bool)GetValue(UseRegexProperty); }
            set { SetValue(UseRegexProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="MatchCase"/>.
        /// </summary>
        public static readonly DependencyProperty MatchCaseProperty =
            DependencyProperty.Register("MatchCase", typeof(bool), typeof(SearchPanel),
                                        new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should be interpreted case-sensitive.
        /// </summary>
        public bool MatchCase
        {
            get { return (bool)GetValue(MatchCaseProperty); }
            set { SetValue(MatchCaseProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="WholeWords"/>.
        /// </summary>
        public static readonly DependencyProperty WholeWordsProperty =
            DependencyProperty.Register("WholeWords", typeof(bool), typeof(SearchPanel),
                                        new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should only match whole words.
        /// </summary>
        public bool WholeWords
        {
            get { return (bool)GetValue(WholeWordsProperty); }
            set { SetValue(WholeWordsProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="SearchPattern"/>.
        /// </summary>
        public static readonly DependencyProperty SearchPatternProperty =
            DependencyProperty.Register("SearchPattern", typeof(string), typeof(SearchPanel),
                                        new FrameworkPropertyMetadata("", SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets the search pattern.
        /// </summary>
        public string SearchPattern
        {
            get { return (string)GetValue(SearchPatternProperty); }
            set { SetValue(SearchPatternProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="MarkerBrush"/>.
        /// </summary>
        public static readonly DependencyProperty MarkerBrushProperty =
            DependencyProperty.Register("MarkerBrush", typeof(Brush), typeof(SearchPanel),
                                        new FrameworkPropertyMetadata(Brushes.LightGreen, MarkerBrushChangedCallback));

        /// <summary>
        /// Gets/sets the Brush used for marking search results in the TextView.
        /// </summary>
        public Brush MarkerBrush
        {
            get { return (Brush)GetValue(MarkerBrushProperty); }
            set { SetValue(MarkerBrushProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="Localization"/>.
        /// </summary>
        public static readonly DependencyProperty LocalizationProperty =
            DependencyProperty.Register("Localization", typeof(Localization), typeof(SearchPanel),
                                        new FrameworkPropertyMetadata(new Localization()));

        /// <summary>
        /// Gets/sets the localization for the SearchPanel.
        /// </summary>
        public Localization Localization
        {
            get { return (Localization)GetValue(LocalizationProperty); }
            set { SetValue(LocalizationProperty, value); }
        }
        #endregion

        static void MarkerBrushChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchPanel panel)
            {
                //SearchPanel panel = d as SearchPanel;
                if (panel != null)
                {
                    panel.renderer.MarkerBrush = (Brush)e.NewValue;
                }
            }
        }

        static SearchPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchPanel), new FrameworkPropertyMetadata(typeof(SearchPanel)));
        }

        ISearchStrategy? strategy;

        static void SearchPatternChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchPanel panel)
            {
                //SearchPanel panel = d as SearchPanel;
                if (panel != null)
                {
                    panel.ValidateSearchText();
                    panel.UpdateSearch();
                }
            }
        }

        void UpdateSearch()
        {
            // only reset as long as there are results
            // if no results are found, the "no matches found" message should not flicker.
            // if results are found by the next run, the message will be hidden inside DoSearch ...
            if (renderer.CurrentResults.Any())
                messageView.IsOpen = false;
            strategy = SearchStrategyFactory.Create(SearchPattern ?? "", !MatchCase, WholeWords, UseRegex ? SearchMode.RegEx : SearchMode.Normal);
            OnSearchOptionsChanged(new SearchOptionsChangedEventArgs(SearchPattern, MatchCase, UseRegex, WholeWords));
            DoSearch(true);
        }

        /// <summary>
        /// Creates a new SearchPanel.
        /// </summary>
        SearchPanel()
        {
        }

        /// <summary>
        /// Creates a SearchPanel and installs it to the TextEditor's TextArea.
        /// </summary>
        /// <remarks>This is a convenience wrapper.</remarks>
        public static SearchPanel Install(TextEditor editor)
        {
            ArgumentNullException.ThrowIfNull(editor);
            //if (editor == null)
            //    throw new ArgumentNullException(nameof(editor));
            return Install(editor.TextArea);
        }

        /// <summary>
        /// Creates a SearchPanel and installs it to the TextArea.
        /// </summary>
        public static SearchPanel Install(TextArea textArea)
        {
            ArgumentNullException.ThrowIfNull(textArea);
            //if (textArea == null)
            //    throw new ArgumentNullException(nameof(textArea));
            SearchPanel panel = new SearchPanel();
            panel.AttachInternal(textArea);
            panel.handler = new SearchInputHandler(textArea, panel);
            textArea.DefaultInputHandler.NestedInputHandlers.Add(panel.handler);
            return panel;
        }

        /// <summary>
        /// Adds the commands used by SearchPanel to the given CommandBindingCollection.
        /// </summary>
        public void RegisterCommands(CommandBindingCollection commandBindings)
        {
            if (handler != null)
            {
                handler.RegisterGlobalCommands(commandBindings);
            }
        }

        /// <summary>
        /// Removes the SearchPanel from the TextArea.
        /// </summary>
        public void Uninstall()
        {
            Close();
            if (textArea == null) return;
            textArea.DocumentChanged -= textArea_DocumentChanged;
            if (currentDocument != null)
                currentDocument.TextChanged -= textArea_Document_TextChanged;
            if (handler != null)
                textArea.DefaultInputHandler.NestedInputHandlers.Remove(handler);
        }

        void AttachInternal(TextArea textArea)
        {
            this.textArea = textArea;
            adorner = new SearchPanelAdorner(textArea, this);
            DataContext = this;

            //renderer
            currentDocument = textArea.Document;
            if (currentDocument != null)
                currentDocument.TextChanged += textArea_Document_TextChanged;
            textArea.DocumentChanged += textArea_DocumentChanged;
            KeyDown += SearchLayerKeyDown;

            this.CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, (sender, e) => FindNext()));
            this.CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, (sender, e) => FindPrevious()));
            this.CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, (sender, e) => Close()));
            IsClosed = true;
        }

        void textArea_DocumentChanged(object? sender, EventArgs e)
        {
            if (textArea == null)
            {
                return;
            }

            if (currentDocument != null)
            {
                currentDocument.TextChanged -= textArea_Document_TextChanged;
            }
            currentDocument = textArea.Document;
            if (currentDocument != null)
            {
                currentDocument.TextChanged += textArea_Document_TextChanged;
                DoSearch(false);
            }
        }

        void textArea_Document_TextChanged(object? sender, EventArgs e)
        {
            DoSearch(false);
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            searchTextBox = Template.FindName("PART_searchTextBox", this) as TextBox;
            //dropdownPopup = Template.FindName("PART_dropdownPopup", this) as Popup;
        }

        void ValidateSearchText()
        {
            if (searchTextBox == null)
                return;

            var be = searchTextBox.GetBindingExpression(TextBox.TextProperty);

            try
            {
                if (be != null)
                    Validation.ClearInvalid(be);

                UpdateSearch();

            }
            catch (SearchPatternException ex)
            {
                var ve = new ValidationError(be.ParentBinding.ValidationRules[0], be, ex.Message, ex);
                Validation.MarkInvalid(be, ve);
            }
        }

        /// <summary>
        /// Reactivates the SearchPanel by setting the focus on the search box and selecting all text.
        /// </summary>
        public void Reactivate()
        {
            if (searchTextBox == null)
                return;
            searchTextBox.Focus();
            searchTextBox.SelectAll();
        }

        /// <summary>
        /// Moves to the next occurrence in the file.
        /// </summary>
        public void FindNext()
        {
            if (textArea == null) return;
            SearchResult result = renderer.CurrentResults.FindFirstSegmentWithStartAfter(textArea.Caret.Offset + 1);
            if (result == null)
                result = renderer.CurrentResults.FirstSegment;
            if (result != null)
            {
                SelectResult(result);
            }
        }

        /// <summary>
        /// Moves to the previous occurrence in the file.
        /// </summary>
        public void FindPrevious()
        {
            if (textArea == null) return;
            SearchResult result = renderer.CurrentResults.FindFirstSegmentWithStartAfter(textArea.Caret.Offset);
            if (result != null)
                result = renderer.CurrentResults.GetPreviousSegment(result);
            if (result == null)
                result = renderer.CurrentResults.LastSegment;
            if (result != null)
            {
                SelectResult(result);
            }
        }

        ToolTip messageView = new ToolTip { Placement = PlacementMode.Bottom, StaysOpen = true, Focusable = false };

        void DoSearch(bool changeSelection)
        {
            if (IsClosed)
                return;
            renderer.CurrentResults.Clear();

            if (strategy == null || textArea == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(SearchPattern))
            {
                int offset = textArea.Caret.Offset;
                if (changeSelection)
                {
                    textArea.ClearSelection();
                }
                // We cast from ISearchResult to SearchResult; this is safe because we always use the built-in strategy
                foreach (SearchResult result in strategy.FindAll(textArea.Document, 0, textArea.Document.TextLength))
                {
                    if (changeSelection && result.StartOffset >= offset)
                    {
                        SelectResult(result);
                        changeSelection = false;
                    }
                    renderer.CurrentResults.Add(result);
                }
                if (!renderer.CurrentResults.Any())
                {
                    messageView.IsOpen = true;
                    messageView.Content = Localization.NoMatchesFoundText;
                    messageView.PlacementTarget = searchTextBox;
                }
                else
                    messageView.IsOpen = false;
            }
            textArea.TextView.InvalidateLayer(KnownLayer.Selection);
        }

        void SelectResult(SearchResult result)
        {
            if (textArea == null) return;
            textArea.Caret.Offset = result.StartOffset;
            textArea.Selection = Selection.Create(textArea, result.StartOffset, result.EndOffset);
            textArea.Caret.BringCaretToView();
            // show caret even if the editor does not have the Keyboard Focus
            textArea.Caret.Show();
        }

        void SearchLayerKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        FindPrevious();
                    else
                        FindNext();
                    if (searchTextBox != null)
                    {
                        var error = Validation.GetErrors(searchTextBox).FirstOrDefault();
                        if (error != null)
                        {
                            messageView.Content = Localization.ErrorText + " " + error.ErrorContent;
                            messageView.PlacementTarget = searchTextBox;
                            messageView.IsOpen = true;
                        }
                    }
                    break;
                case Key.Escape:
                    e.Handled = true;
                    Close();
                    break;
            }
        }

        /// <summary>
        /// Gets whether the Panel is already closed.
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Closes the SearchPanel.
        /// </summary>
        public void Close()
        {
            bool hasFocus = IsKeyboardFocusWithin;

            if (textArea == null) return;

            var layer = AdornerLayer.GetAdornerLayer(textArea);
            if (layer != null && adorner != null)
                layer.Remove(adorner);
            //if (dropdownPopup != null)
            //	dropdownPopup.IsOpen = false;
            messageView.IsOpen = false;
            textArea.TextView.BackgroundRenderers.Remove(renderer);
            if (hasFocus)
                textArea.Focus();
            IsClosed = true;

            // Clear existing search results so that the segments don't have to be maintained
            renderer.CurrentResults.Clear();
        }

        /// <summary>
        /// Opens the an existing search panel.
        /// </summary>
        public void Open()
        {
            if (!IsClosed) return;
            if (textArea == null) return;
            var layer = AdornerLayer.GetAdornerLayer(textArea);
            if (layer != null && adorner != null)
                layer.Add(adorner);
            textArea.TextView.BackgroundRenderers.Add(renderer);
            IsClosed = false;
            DoSearch(false);
        }

        /// <summary>
        /// Fired when SearchOptions are changed inside the SearchPanel.
        /// </summary>
        public event EventHandler<SearchOptionsChangedEventArgs>? SearchOptionsChanged;

        /// <summary>
        /// Raises the <see cref="SearchOptionsChanged" /> event.
        /// </summary>
        protected virtual void OnSearchOptionsChanged(SearchOptionsChangedEventArgs e)
        {
            SearchOptionsChanged?.Invoke(this, e);
        }

        #region ColorScheme

        public event DependencyPropertyChangedEventHandler? ColorSchemeChanged;

        public static DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register("ColorScheme", typeof(ColorScheme), typeof(SearchPanel),
            new FrameworkPropertyMetadata(new ColorScheme(), new PropertyChangedCallback(OnColorSchemeChanged)));

        public static void OnColorSchemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorScheme cs = (e.NewValue as ColorScheme)!;

            if (d is SearchPanel s)
            {
                s.ColorSchemeChanged?.Invoke(d, e);
                s.ApplyColorScheme(cs);
            }
        }

        public ColorScheme ColorScheme
        {
            get => (ColorScheme)GetValue(ColorSchemeProperty);
            set => SetValue(ColorSchemeProperty, value);
        }

        public void ApplyColorScheme(ColorScheme cs)
        {
            if (cs != ColorScheme)
            {
                ColorScheme = cs;
                return;
            }
        }


        #endregion
    }

    ///// <summary>
    ///// EventArgs for <see cref="SearchPanel.SearchOptionsChanged"/> event.
    ///// </summary>
    //public class SearchOptionsChangedEventArgs : EventArgs
    //{
    //	/// <summary>
    //	/// Gets the search pattern.
    //	/// </summary>
    //	public string SearchPattern { get; private set; }

    //	/// <summary>
    //	/// Gets whether the search pattern should be interpreted case-sensitive.
    //	/// </summary>
    //	public bool MatchCase { get; private set; }

    //	/// <summary>
    //	/// Gets whether the search pattern should be interpreted as regular expression.
    //	/// </summary>
    //	public bool UseRegex { get; private set; }

    //	/// <summary>
    //	/// Gets whether the search pattern should only match whole words.
    //	/// </summary>
    //	public bool WholeWords { get; private set; }

    //	/// <summary>
    //	/// Creates a new SearchOptionsChangedEventArgs instance.
    //	/// </summary>
    //	public SearchOptionsChangedEventArgs(string searchPattern, bool matchCase, bool useRegex, bool wholeWords)
    //	{
    //		this.SearchPattern = searchPattern;
    //		this.MatchCase = matchCase;
    //		this.UseRegex = useRegex;
    //		this.WholeWords = wholeWords;
    //	}
    //}

    class SearchPanelAdorner : Adorner
    {
        SearchPanel panel;

        public SearchPanelAdorner(TextArea textArea, SearchPanel panel)
            : base(textArea)
        {
            this.panel = panel;
            AddVisualChild(panel);
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException();
            return panel;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            panel.Arrange(new Rect(new Point(0, 0), finalSize));
            return new Size(panel.ActualWidth, panel.ActualHeight);
        }
    }
}
