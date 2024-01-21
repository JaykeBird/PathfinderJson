using PathfinderJson.Ild;
using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

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
        /// Generate a list of UI elements, each one corresponding to the element in the source item enumerable. Make sure the <c>SheetClassType</c> and <c>DisplayElementType</c> properties are already set.
        /// </summary>
        /// <typeparam name="T">The type of the source item.</typeparam>
        /// <param name="items">The list/enumerable of source items to load.</param>
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

        public List<T> GetItems<T>()
        {
            if (typeof(T) != SheetClassType) throw new ArgumentException("Passed in generic data type does not match SheetClassType");
            if (propertyNames.Count == 0) throw new InvalidOperationException("SheetClassType has no properties, or was not set.");

            List<T> items = new List<T>();

            foreach (SelectableListItem item in selPanel.Items)
            {
                //Dictionary<string, object> propVals = item.GetAllProperties();

                var newBase = Activator.CreateInstance(typeof(T));
                if (newBase == null) throw new ArgumentNullException(nameof(T), "Passed in generic data type cannot be created via reflection");
                Type tt = typeof(T);
                foreach (IldPropertyInfo property in propertyNames)
                {
                    PropertyInfo? pi = tt.GetProperty(property.Name);

                    if (pi != null)
                    {
                        pi.SetValue(newBase, item.GetPropertyValue(property));
                    }
                }

                items.Add((T)newBase);
            }

            return items;
        }

        /// <summary>
        /// Generate a list of property info from a source type. The IldDisplayAttribute is handled here.
        /// </summary>
        /// <param name="type">The source type to load.</param>
        /// <returns>A list of property info.</returns>
        public List<IldPropertyInfo> ListProperties(Type type)
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
                else
                {
                    // just skip properties that we don't support
                    continue;
                    //throw new NotSupportedException("This property uses a type that isn't supported by the ItemListDisplay.");
                }

                IldPropertyInfo prop = new IldPropertyInfo(property.Name, ildType, name);
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
            Dictionary<IldPropertyInfo, object> props = new Dictionary<IldPropertyInfo, object>();
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
                        default:
                            val = "";
                            break;
                    }
                }

                props.Add(prop, val);
            }

            return props;
        }

        private void LoadFilterMenu()
        {
            var cm = new SolidShineUi.ContextMenu();

            foreach (var item in propertyNames)
            {
                MenuItem mi = new MenuItem();
                mi.Header = item.DisplayName;
                mi.Tag = item;

                MenuItem mcf = new MenuItem();
                mcf.Header = "Clear Filter";

                switch (item.IldType)
                {
                    case IldType.String:
                        MenuItem msi1 = new MenuItem();
                        msi1.Header = "Contains...";
                        msi1.Click += (s, e) => StringFilterAction(FilterType.STRING_CONTAINS, item, mi, mcf);
                        mi.Items.Add(msi1);

                        MenuItem msi2 = new MenuItem();
                        msi2.Header = "Does Not Contain...";
                        msi2.Click += (s, e) => StringFilterAction(FilterType.STRING_NOT_CONTAINS, item, mi, mcf);
                        mi.Items.Add(msi2);

                        MenuItem msi3 = new MenuItem();
                        msi3.Header = "Starts With...";
                        msi3.Click += (s, e) => StringFilterAction(FilterType.STRING_STARTS_WITH, item, mi, mcf);
                        mi.Items.Add(msi3);

                        MenuItem msi4 = new MenuItem();
                        msi4.Header = "Matches (Exactly)...";
                        msi4.Click += (s, e) => StringFilterAction(FilterType.STRING_MATCHES, item, mi, mcf);
                        mi.Items.Add(msi4);
                        break;
                    case IldType.Integer:
                        ListNumberMenuItems(item, mi, mcf);
                        break;
                    case IldType.Double:
                        ListNumberMenuItems(item, mi, mcf);
                        break;
                    case IldType.Boolean:
                        MenuItem mbi1 = new MenuItem();
                        mbi1.Header = "True (Checked)";
                        mbi1.Click += (s, e) => ApplyBooleanTrueFilter(item, mi, mcf);
                        mi.Items.Add(mbi1);

                        MenuItem mbi2 = new MenuItem();
                        mbi2.Header = "False (Unchecked)";
                        mbi2.Click += (s, e) => ApplyBooleanFalseFilter(item, mi, mcf);
                        mi.Items.Add(mbi2);
                        break;
                    default:
                        MenuItem mdi1 = new MenuItem();
                        mdi1.Header = "Data type not supported for filtering";
                        mdi1.IsEnabled = false;
                        mi.Items.Add(mdi1);
                        break;
                }

                mi.Items.Add(new Separator());

                mcf.IsEnabled = false;
                mcf.Click += (s, e) => ClearFilter(item, mi, mcf);
                mi.Items.Add(mcf);

                //MenuItem smi = new MenuItem();
                //smi.Header = item.DisplayName;
                //smi.Tag = item;
                //smi.Click += (s, e) => { Sort(item); };

                //btnSort.Items.Add(smi);

                cm.Items.Add(mi);
            }

            cm.Items.Add(new Separator());

            MenuItem mcli = new MenuItem();
            mcli.Header = "Clear All Filters";
            mcli.Click += (s, e) => { ClearAllFilters(); };
            cm.Items.Add(mcli);

            btnFilter.Menu = cm;

            void ListNumberMenuItems(IldPropertyInfo item, MenuItem mi, MenuItem cancelItem)
            {
                if (item.MinValue != null && item.MaxValue != null)
                {
                    int min = item.MinValue.Value;
                    int max = item.MaxValue.Value;

                    if (max - min < 10 && max - min > 0)
                    {
                        for (int i = min; i <= max; i++)
                        {
                            MenuItem mni = new MenuItem();
                            mni.Click += (s, e) => ApplyIntegerMatchesFilter(item, i, mi, cancelItem);
                            mni.Header = i.ToString();
                            mi.Items.Add(mni);
                        }
                    }
                    else
                    {
                        MenuItem mni1 = new MenuItem();
                        mni1.Click += (s, e) => NumberFilterAction(FilterType.NUMBER_EQUALS, item, mi, cancelItem);
                        mni1.Header = "Equals...";
                        mi.Items.Add(mni1);
                    }

                    mi.Items.Add(new Separator());
                }
                else
                {
                    MenuItem mni3 = new MenuItem();
                    mni3.Click += (s, e) => NumberFilterAction(FilterType.NUMBER_EQUALS, item, mi, cancelItem);
                    mni3.Header = "Equals...";
                    mi.Items.Add(mni3);
                }

                MenuItem mni5 = new MenuItem();
                mni5.Click += (s, e) => NumberFilterAction(FilterType.NUMBER_BETWEEN, item, mi, cancelItem);
                mni5.Header = "Is Between...";
                mi.Items.Add(mni5);

                MenuItem mni6 = new MenuItem();
                mni6.Click += (s, e) => NumberFilterAction(FilterType.NUMBER_NOT_BETWEEN, item, mi, cancelItem);
                mni6.Header = "Is Not Between...";
                mi.Items.Add(mni6);
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
            sid.Description = (int)action switch
            {
                0 => "Match items that contain this value:",
                1 => "Match items that do not contain this value:",
                2 => "Match items that start with this value:",
                3 => "Match items that exactly match this value:",
                _ => "Enter the value to filter by:"
            };

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
                FilterType.NUMBER_EQUALS => "Match items that have this exact number for " + property.DisplayName + ":",
                FilterType.NUMBER_BETWEEN => "Match items that are between these values for " + property.DisplayName + ":",
                FilterType.NUMBER_NOT_BETWEEN => "Match items that are not between these values for " + property.DisplayName + ":",
                _ => "Enter the value to filter by:"
            };

            if (property.IldType == IldType.Integer)
            {
                sid.Decimals = 0;
            }
            else
            {
                sid.Decimals = 3;
            }

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

        //void Sort(IldPropertyInfo? sortProp)
        //{
        //    return;
        //}

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

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnRemoveMultiple_Click(object sender, RoutedEventArgs e)
        {
            selPanel.RemoveSelectedItems();
        }
    }
}
