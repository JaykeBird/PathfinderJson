using PathfinderJson.Ild;
using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Reflection;

namespace PathfinderJson.Ild
{
    /// <summary>
    /// Interaction logic for ItemListDisplay.xaml
    /// </summary>
    public partial class ItemListDisplay : UserControl
    {
        public ItemListDisplay()
        {
            InitializeComponent();
        }

        public event EventHandler? ContentChanged;

        #region ColorScheme

        public event DependencyPropertyChangedEventHandler? ColorSchemeChanged;

        public static DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register("ColorScheme", typeof(ColorScheme), typeof(ItemListDisplay),
            new FrameworkPropertyMetadata(new ColorScheme(), new PropertyChangedCallback(OnColorSchemeChanged)));

        public static void OnColorSchemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorScheme cs = (e.NewValue as ColorScheme)!;

            if (d is ItemListDisplay s)
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

            BorderBrush = cs.BorderColor.ToBrush();
        }

        #endregion

        /// <summary>
        /// Get or set the title displayed at the top of the ItemListDisplay's UI.
        /// </summary>
        public string Title
        {
            get => txtTitle.Text;
            set => txtTitle.Text = value;
        }

        /// <summary>
        /// Get or set the type of the source item, which serves as the backend.
        /// </summary>
        public Type? SheetClassType
        {
            get => sheetType;
            set
            {
                sheetType = value;
                if (value != null)
                {
                    propertyNames = ListProperties(value);
                    LoadSortMenu();
                    LoadFilterMenu();
                }
                else
                {
                    propertyNames.Clear();
                    btnFilter.Menu = null;
                }
            }
        }

        private Type? sheetType;
        private Type? displayElement;

        private static Type SELECTABLE_ITEM_TYPE = typeof(SelectableListItem);

        private List<IldPropertyInfo> propertyNames = new List<IldPropertyInfo>();

        /// <summary>
        /// Get or set the type of the UI element that is used to display the source item <c>SheetClassType</c>. This type must inherit from <see cref="SelectableListItem"/>.
        /// </summary>
        public Type? DisplayElementType
        {
            get
            {
                return displayElement;
            }
            set
            {
                displayElement = SELECTABLE_ITEM_TYPE.IsAssignableFrom(value)
                    ? value
                    : throw new ArgumentException("Entered type must derive from the SelectableListItem type.");
            }
        }

        /// <summary>
        /// Generate a list of UI elements, each one corresponding to the element in the source item enumerable. 
        /// Make sure the <c>SheetClassType</c> and <c>DisplayElementType</c> properties are already set.
        /// </summary>
        /// <typeparam name="T">The type of the source item.</typeparam>
        /// <param name="items">The list/enumerable of source items to load.</param>
        /// <exception cref="ArgumentException"><typeparamref name="T"/> is not the same as <see cref="SheetClassType"/></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void LoadList<T>(IEnumerable<T> items)
        {
            if (typeof(T) != SheetClassType)
            {
                throw new ArgumentException("Designated generic type does not match SheetClassType property.");
            }

            if (DisplayElementType == null)
            {
                throw new InvalidOperationException("DisplayElementType is not set");
            }

            selPanel.Items.Clear();

            foreach (T item in items)
            {
                var newItem = Activator.CreateInstance(DisplayElementType);

                SelectableListItem sli = (SelectableListItem)newItem!;

                sli.LoadValues(GetAllPropertyValues(item));

                sli.RequestDelete += sli_RequestDelete;
                sli.RequestMoveDown += sli_RequestMoveDown;
                sli.RequestMoveUp += sli_RequestMoveUp;

                selPanel.Items.Add(sli);
            }
        }

        /// <summary>
        /// Generate a list of items from the UI elements currently in this ItemListDisplay (in other words, going from UI back to the data classes). 
        /// <typeparamref name="T"/> must be the same type as <see cref="SheetClassType"/>.
        /// </summary>
        /// <typeparam name="T">MUST be the same type as <see cref="SheetClassType"/></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> is not the same as <see cref="SheetClassType"/></exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="SheetClassType"/> is not valid (i.e. has no properties, or was not set)</exception>
        /// <exception cref="TargetException">The type <typeparamref name="T"/> cannot be created via reflection</exception>
        public List<T> GetItems<T>() where T : new()
        {
            if (typeof(T) != SheetClassType) throw new ArgumentException("Passed in generic data type does not match SheetClassType");
            if (propertyNames.Count == 0) throw new InvalidOperationException("SheetClassType has no properties, or was not set.");

            List<T> items = new();

            foreach (SelectableListItem item in selPanel.Items.OfType<SelectableListItem>())
            {
                //Dictionary<string, object> propVals = item.GetAllProperties();

                var newBase = Activator.CreateInstance(typeof(T))
                    /* if newBase == null */ ?? throw new TargetException("Passed in generic data type cannot be created via reflection");
                Type tt = typeof(T);
                foreach (IldPropertyInfo property in propertyNames)
                {
                    PropertyInfo? pi = tt.GetProperty(property.Name);
                    pi?.SetValue(newBase, item.GetPropertyValue(property));
                }

                items.Add((T)newBase);
            }

            return items;
        }

        /// <summary>
        /// Generate a list of <see cref="IldPropertyInfo"/> items from a class type, 
        /// which is done by reading properties via reflection and checking the <see cref="IldDisplayAttribute"/>.
        /// </summary>
        /// <param name="type">The class type to load.</param>
        /// <returns>A list of <see cref="IldPropertyInfo"/> items for each property that was recognized.</returns>
        public static List<IldPropertyInfo> ListProperties(Type type)
        {
            List<IldPropertyInfo> props = new List<IldPropertyInfo>();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string name = property.Name;
                var attr = property.GetCustomAttribute<IldDisplayAttribute>();

                int? minValue = null;
                int? maxValue = null;

                if (attr != null)
                {
                    // we will skip properties that are marked to ignore
                    if (attr.Ignore) continue;

                    if (attr.Name != null) name = attr.Name;
                    minValue = attr.MinValue;
                    maxValue = attr.MaxValue;
                }

                Type pt = property.PropertyType;

                IldType ildType;

                if (pt == typeof(string))
                {
                    ildType = IldType.String;
                }
                else if (pt == typeof(bool))
                {
                    ildType = IldType.Boolean;
                }
                else if (pt == typeof(int))
                {
                    ildType = IldType.Integer;
                }
                else if (pt == typeof(double))
                {
                    ildType = IldType.Double;
                }
                else if (pt.IsEnum)
                {
                    ildType = IldType.Enum;
                }
                else
                {
                    // just skip properties that we don't support
                    continue;
                    //throw new NotSupportedException("This property uses a type that isn't supported by the ItemListDisplay.");
                }

                IldPropertyInfo prop = new(property.Name, ildType, pt, name);
                prop.MinValue = minValue;
                prop.MaxValue = maxValue;
                props.Add(prop);
            }

            return props;
        }

        /// <summary>
        /// Generate the dictionary of properties from the specified type, alongside the value of these properties from a source item.
        /// </summary>
        /// <typeparam name="T">the type of the object to load properties and values from (this should match <see cref="SheetClassType"/>)</typeparam>
        /// <param name="item">the object to load properties and values from</param>
        /// <returns></returns>
        private Dictionary<IldPropertyInfo, object> GetAllPropertyValues<T>(T item)
        {
            Dictionary<IldPropertyInfo, object> props = new();
            Type type = typeof(T);

            foreach (IldPropertyInfo prop in propertyNames)
            {
                PropertyInfo? property = type.GetProperty(prop.Name);

                // failsafe in case there's some weird mixup and this property isn't actually present
                if (property == null) continue;

                //var attr = property.GetCustomAttribute<IldDisplayAttribute>();

                //if (attr != null)
                //{
                //    if (attr.Ignore) continue;
                //}

                object? val = property.GetValue(item);

                if (val == null)
                {
                    // set default values if val is null
                    switch (prop.IldType)
                    {
                        case IldType.String:
                            val = "";
                            break;
                        case IldType.Integer:
                            val = 0;
                            break;
                        case IldType.Double:
                            val = 0d;
                            break;
                        case IldType.Boolean:
                            val = false;
                            break;
                        case IldType.Enum:
                            val = Enum.Parse(property.PropertyType, Enum.GetNames(property.PropertyType)[0]);
                            break;
                        default:
                            val = "";
                            break;
                    }
                }

                props.Add(prop, val);
            }

            return props;
        }

        private void LoadSortMenu()
        {
            var cm = new SolidShineUi.ContextMenu();

            foreach (var item in propertyNames)
            {
                MenuItem mi = new();
                mi.Header = item.DisplayName;
                mi.Tag = item;

                cm.Items.Add(mi);
            }

            cm.Items.Add(new Separator());

            MenuItem msd1 = new MenuItem();
            msd1.Header = "Ascending";
            msd1.Click += (s, e) => { };
            cm.Items.Add(msd1);

            MenuItem msd2 = new MenuItem();
            msd2.Header = "Descending";
            msd2.Click += (s, e) => { };
            cm.Items.Add(msd2);

            btnSort.Menu = cm;
        }

        private void LoadFilterMenu()
        {
            var cm = new SolidShineUi.ContextMenu();

            foreach (var item in propertyNames)
            {
                MenuItem mi = new();
                mi.Header = item.DisplayName;
                mi.Tag = item;

                MenuItem mcf = new();
                mcf.Header = "Clear Filter";
                mcf.Click += (s, e) => ClearFilter(item, mi, mcf);
                mcf.IsEnabled = false;

                switch (item.IldType)
                {
                    case IldType.String:
                        mi.Items.Add(CreateMenuItem("Contains...", (s, e) => StringFilterAction(FilterType.STRING_CONTAINS, item, mi, mcf)));
                        mi.Items.Add(CreateMenuItem("Does Not Contain...", (s, e) => StringFilterAction(FilterType.STRING_NOT_CONTAINS, item, mi, mcf)));
                        mi.Items.Add(CreateMenuItem("Starts With...", (s, e) => StringFilterAction(FilterType.STRING_STARTS_WITH, item, mi, mcf)));
                        mi.Items.Add(CreateMenuItem("Matches Exactly...", (s, e) => StringFilterAction(FilterType.STRING_MATCHES, item, mi, mcf)));
                        break;
                    case IldType.Integer:
                        ListNumberMenuItems(item, mi, mcf);
                        break;
                    case IldType.Double:
                        ListNumberMenuItems(item, mi, mcf);
                        break;
                    case IldType.Boolean:
                        mi.Items.Add(CreateMenuItem("True (Checked)", (s, e) => ApplyBooleanTrueFilter(item, mi, mcf)));
                        mi.Items.Add(CreateMenuItem("False (Unchecked)", (s, e) => ApplyBooleanFalseFilter(item, mi, mcf)));
                        break;
                    case IldType.Enum:
                        ListEnumOptions(item, mi, mcf);
                        break;
                    default:
                        MenuItem mdi1 = new MenuItem();
                        mdi1.Header = "Data type not supported for filtering";
                        mdi1.IsEnabled = false;
                        mi.Items.Add(mdi1);
                        break;
                }

                mi.Items.Add(new Separator());
                mi.Items.Add(mcf);

                //MenuItem smi = new MenuItem();
                //smi.Header = item.DisplayName;
                //smi.Tag = item;
                //smi.Click += (s, e) => { Sort(item); };

                //btnSort.Items.Add(smi);

                cm.Items.Add(mi);

            } // foreach (var item in propertyNames)

            cm.Items.Add(new Separator());

            MenuItem mcli = new MenuItem();
            mcli.Header = "Clear All Filters";
            mcli.Click += (s, e) => { ClearAllFilters(); };
            cm.Items.Add(mcli);

            btnFilter.Menu = cm;

            MenuItem CreateMenuItem(string title, RoutedEventHandler clickHandler)
            {
                MenuItem mi = new();
                mi.Header = title;
                mi.Click += clickHandler;
                return mi;
            }

            void ListNumberMenuItems(IldPropertyInfo item, MenuItem mi, MenuItem cancelItem)
            {
                if (item.MinValue != null && item.MaxValue != null && item.IldType == IldType.Integer)
                {
                    int min = item.MinValue.Value;
                    int max = item.MaxValue.Value;

                    if (max - min < 10 && max - min > 0) // only generate a menu item list if there's 10 or less valid numbers
                    {
                        for (int i = min; i <= max; i++)
                        {
                            MenuItem mni = CreateMenuItem(i.ToString(), (s, e) => ApplyIntegerMatchesFilter(item, i, mi, cancelItem));
                            mi.Items.Add(mni);
                        }
                    }
                    else
                    {
                        MenuItem mni1 = CreateMenuItem("Equals...", (s, e) => NumberFilterAction(FilterType.NUMBER_EQUALS, item, mi, cancelItem));
                        mi.Items.Add(mni1);
                    }

                    mi.Items.Add(new Separator());
                }
                else
                {
                    MenuItem mni3 = CreateMenuItem("Equals..." ,(s, e) => NumberFilterAction(FilterType.NUMBER_EQUALS, item, mi, cancelItem));
                    mi.Items.Add(mni3);
                }

                MenuItem mni5 = CreateMenuItem("Is Between...", (s, e) => NumberFilterAction(FilterType.NUMBER_BETWEEN, item, mi, cancelItem));
                mi.Items.Add(mni5);

                MenuItem mni6 = CreateMenuItem("Is Not Between...", (s, e) => NumberFilterAction(FilterType.NUMBER_NOT_BETWEEN, item, mi, cancelItem));
                mi.Items.Add(mni6);
            }

            void ListEnumOptions(IldPropertyInfo item, MenuItem mi, MenuItem cancelItem)
            {
                Type actualEnumType = item.ActualPropertyType;
                string[] values = Enum.GetNames(actualEnumType);

                foreach (string val in values)
                {
                    MenuItem mni = CreateMenuItem(val, (s, e) => ApplyEnumMatchesFilter(item, val, mi, cancelItem));
                    mi.Items.Add(mni);
                }

                mi.Items.Add(new Separator());
                // not sure if I'll actually need/want this menu item, but for now, I'll keep it here
                mi.Items.Add(CreateMenuItem("Matches...", (s, e) => StringFilterAction(FilterType.ENUM_MATCHES, item, mi, cancelItem)));
            }
        }

        #region Filter menu options

        public void ApplyFilter(string propertyName, FilterType filterType, string filterValue)
        {
            IldPropertyInfo? prop = propertyNames.FirstOrDefault((p) => p.Name == propertyName);
            if (prop == null) return;
            ApplyFilter(prop, filterType, filterValue);
        }

        public void ApplyFilter(IldPropertyInfo property, FilterType filterType, string filterValue)
        {
            property.Filter = new IldPropertyFilter(filterType, filterValue);
            ApplyFilters();
        }

        public void ClearAllFilters()
        {
            foreach (SelectableUserControl item in selPanel.Items)
            {
                item.Visibility = Visibility.Visible;
            }

            foreach (IldPropertyInfo prop in propertyNames)
            {
                prop.Filter = null;
            }

            foreach (object? o in btnFilter.Menu!.Items)
            {
                if (o is MenuItem mi)
                {
                    mi.IsChecked = false;
                }
            }
        }

        private void StringFilterAction(FilterType action, IldPropertyInfo property, MenuItem baseItem, MenuItem cancelItem)
        {
            StringInputDialog sid = new StringInputDialog();
            sid.Title = "Enter String Filter";
            sid.Description = action switch
            {
                FilterType.STRING_CONTAINS => "Show items where " + property.DisplayName + " contains:",
                FilterType.STRING_NOT_CONTAINS => "Show items where " + property.DisplayName + " does not contain:",
                FilterType.STRING_STARTS_WITH => "Show items where " + property.DisplayName + " starts with:",
                FilterType.STRING_MATCHES => "Show items where " + property.DisplayName + " exactly matches:",
                FilterType.ENUM_MATCHES => "Show items where " + property.DisplayName + " exactly matches:",
                _ => "Enter the value to filter by:"
            };

            if (action == FilterType.ENUM_MATCHES)
            {
                sid.ValidationFunction = (s) =>
                {
                    return property.ActualPropertyType.IsEnum && Enum.TryParse(property.ActualPropertyType, s, true, out _);
                };
                sid.ValidationFailureString = "Not a valid value for " + property.DisplayName;
            }

            sid.ShowDialog();
            if (sid.DialogResult)
            {
                property.Filter = new IldPropertyFilter(action, sid.Value);
                baseItem.IsChecked = true;
                cancelItem.IsEnabled = true;

                ApplyFilters();
            }
            else
            {
                return;
            }
        }

        private void ApplyBooleanTrueFilter(IldPropertyInfo property, MenuItem baseItem, MenuItem cancelItem)
        {
            property.Filter = new IldPropertyFilter(FilterType.BOOLEAN_TRUE, "TRUE");
            baseItem.IsChecked = true;
            cancelItem.IsEnabled = true;

            ApplyFilters();
        }

        private void ApplyBooleanFalseFilter(IldPropertyInfo property, MenuItem baseItem, MenuItem cancelItem)
        {
            property.Filter = new IldPropertyFilter(FilterType.BOOLEAN_FALSE, "FALSE");
            baseItem.IsChecked = true;
            cancelItem.IsEnabled = true;

            ApplyFilters();
        }

        private void NumberFilterAction(FilterType action, IldPropertyInfo property, MenuItem baseItem, MenuItem cancelItem)
        {
            NumberInputDialog sid = new NumberInputDialog();
            sid.Title = "Enter Number Filter";
            sid.Description = action switch
            {
                FilterType.NUMBER_EQUALS => "Show items where " + property.DisplayName + " is exactly:",
                FilterType.NUMBER_BETWEEN => "Show items where " + property.DisplayName + " is between:",
                FilterType.NUMBER_NOT_BETWEEN => "Show items where " + property.DisplayName + " is no between:",
                _ => "Enter the value to filter by:"
            };

            sid.Decimals = property.IldType == IldType.Integer ? 0 : 3; // if an integer, let's not get decimal numbers involved

            if (property.MinValue != null) sid.MinValue = (double)property.MinValue;
            if (property.MaxValue != null) sid.MaxValue = (double)property.MaxValue;

            if (action == FilterType.NUMBER_EQUALS)
            {
                sid.ShowDialog();
                if (sid.DialogResult)
                {
                    property.Filter = new IldPropertyFilter(action, sid.Value.ToString());
                    baseItem.IsChecked = true;
                    cancelItem.IsEnabled = true;

                    ApplyFilters();
                }
                else
                {
                    return;
                }
            }
            else
            {
                sid.DisplayBetweenControls = true;
                sid.ShowDialog();
                if (sid.DialogResult)
                {
                    property.Filter = new IldPropertyFilter(action, sid.BetweenMinimum.ToString() + "-" + sid.BetweenMaximum.ToString());
                    baseItem.IsChecked = true;
                    cancelItem.IsEnabled = true;

                    ApplyFilters();
                }
                else
                {
                    return;
                }
            }
        }

        private void ApplyIntegerMatchesFilter(IldPropertyInfo property, int match, MenuItem baseItem, MenuItem cancelItem)
        {
            property.Filter = new IldPropertyFilter(FilterType.NUMBER_EQUALS, match.ToString());
            baseItem.IsChecked = true;
            cancelItem.IsEnabled = true;

            ApplyFilters();
        }

        private void ApplyEnumMatchesFilter(IldPropertyInfo property, string match, MenuItem baseItem, MenuItem cancelItem)
        {
            property.Filter = new IldPropertyFilter(FilterType.ENUM_MATCHES, match);
            baseItem.IsChecked = true;
            cancelItem.IsEnabled = true;

            ApplyFilters();
        }

        private void ClearFilter(IldPropertyInfo property, MenuItem baseItem, MenuItem cancelItem)
        {
            property.Filter = null;
            baseItem.IsChecked = false;
            cancelItem.IsEnabled = false;
        }

        #endregion

        void ApplyFilters()
        {
            foreach (SelectableListItem item in selPanel.Items.Cast<SelectableListItem>())
            {
                item.Visibility = Visibility.Visible;

                if (!propertyNames.All((p) => item.PropertyPassesFilter(p)))
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }
        }


        #region Item Event Handlers

        private void sli_ContentChanged(object? sender, EventArgs e)
        {
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void sli_RequestMoveUp(object? sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if (sender is SelectableListItem sli)
            {
                selPanel.MoveItemUp(selPanel.Items.IndexOf(sli));

                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void sli_RequestMoveDown(object? sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if (sender is SelectableListItem sli)
            {
                selPanel.MoveItemDown(selPanel.Items.IndexOf(sli));

                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void sli_RequestDelete(object? sender, EventArgs e)
        {
            if (sender is SelectableListItem sli)
            {
                selPanel.Items.Remove(sli);

                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Toolbar / Menu

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayElementType == null) return;

            var newItem = Activator.CreateInstance(DisplayElementType);

            SelectableListItem sli = (SelectableListItem)newItem!;

            sli.RequestDelete += sli_RequestDelete;
            sli.RequestMoveDown += sli_RequestMoveDown;
            sli.RequestMoveUp += sli_RequestMoveUp;
            sli.ContentChanged += sli_ContentChanged;

            selPanel.Items.Add(sli);

            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void btnDeselect_Click(object sender, RoutedEventArgs e)
        {
            selPanel.Items.ClearSelection();
        }

        private void btnShowHide_Click(object sender, RoutedEventArgs e)
        {
            if (rowButtons.MinHeight > 0)
            {
                // hide list
                rowButtons.MinHeight = 0;
                rowButtons.Height = new GridLength(0);

                rowPanel.MinHeight = 0;
                rowPanel.Height = new GridLength(0);

                imgShowHide.ImageName = "DownArrow";
                txtShowHide.Text = "Show List";
            }
            else
            {
                // show list
                rowButtons.MinHeight = 32;
                rowButtons.Height = new GridLength(1, GridUnitType.Auto);

                rowPanel.MinHeight = 20;
                rowPanel.Height = new GridLength(1, GridUnitType.Star);

                imgShowHide.ImageName = "UpArrow";
                txtShowHide.Text = "Hide List";
            }
        }

        private void btnRemoveMultiple_Click(object sender, RoutedEventArgs e)
        {
            selPanel.RemoveSelectedItems();
        }


        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                foreach (SelectableListItem item in selPanel.Items.Cast<SelectableListItem>())
                {
                    item.Visibility = Visibility.Visible;

                    if (!propertyNames.All((p) => item.MatchesSearchTerm(p, txtSearch.Text)))
                    {
                        item.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                foreach (SelectableListItem item in selPanel.Items.Cast<SelectableListItem>())
                {
                    item.Visibility = Visibility.Visible;
                }
            }
        }

        #endregion
    }
}
