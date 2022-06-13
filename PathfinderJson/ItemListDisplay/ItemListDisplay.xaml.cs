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
                if (SELECTABLE_ITEM_TYPE.IsAssignableFrom(value))
                {
                    displayElement = value;
                }
                else
                {
                    throw new ArgumentException("Entered type must derive from the SelectableListItem type.");
                }
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

        //private List<string> ListProperties<T>(T item)
        //{
        //    Type type = typeof(T);
        //    return ListProperties(type);
        //}

        /// <summary>
        /// Generate a list of property info from a source type. The IldDisplayAttribute is handled here.
        /// </summary>
        /// <param name="type">The source type to load.</param>
        /// <returns>A list of property info.</returns>
        private List<IldPropertyInfo> ListProperties(Type type)
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
        /// Generate the dictionary of properties from the <c>SheetClassType</c>, alongside the value of these properties from a source item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
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
                        msi1.Click += (s, e) => { StringFilterAction(STRING_CONTAINS, item, mi, mcf); };
                        mi.Items.Add(msi1);

                        MenuItem msi2 = new MenuItem();
                        msi2.Header = "Does Not Contain...";
                        msi2.Click += (s, e) => { StringFilterAction(STRING_NOT_CONTAINS, item, mi, mcf); };
                        mi.Items.Add(msi2);

                        MenuItem msi3 = new MenuItem();
                        msi3.Header = "Starts With...";
                        msi3.Click += (s, e) => { StringFilterAction(STRING_STARTS_WITH, item, mi, mcf); };
                        mi.Items.Add(msi3);

                        MenuItem msi4 = new MenuItem();
                        msi4.Header = "Matches (Exactly)...";
                        msi4.Click += (s, e) => { StringFilterAction(STRING_MATCHES, item, mi, mcf); };
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
                        mi.Items.Add(mbi1);

                        MenuItem mbi2 = new MenuItem();
                        mbi2.Header = "False (Unchecked)";
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
                mi.Items.Add(mcf);

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
                            mni.Header = i.ToString();
                            mi.Items.Add(mni);
                        }
                    }
                    else
                    {
                        MenuItem mni1 = new MenuItem();
                        mni1.Header = "Equals...";
                        mi.Items.Add(mni1);
                    }

                    mi.Items.Add(new Separator());

                    MenuItem mni2 = new MenuItem();
                    mni2.Header = "Is Between...";
                    mi.Items.Add(mni2);

                    MenuItem mni3 = new MenuItem();
                    mni3.Header = "Is Not Between...";
                    mi.Items.Add(mni3);
                }
                else
                {
                    MenuItem mni4 = new MenuItem();
                    mni4.Header = "Equals...";
                    mi.Items.Add(mni4);

                    MenuItem mni5 = new MenuItem();
                    mni5.Header = "Is Between...";
                    mi.Items.Add(mni5);

                    MenuItem mni6 = new MenuItem();
                    mni6.Header = "Is Not Between...";
                    mi.Items.Add(mni6);
                }
            }
        }

        #region Filter menu options

        public void ClearAllFilters()
        {
            foreach (SelectableUserControl item in selPanel.Items)
            {
                item.Visibility = Visibility.Visible;
            }

            foreach (object? o in btnFilter.Menu!.Items)
            {
                if (o is MenuItem mi)
                {
                    mi.IsChecked = false;
                }
            }
        }

        private const int STRING_CONTAINS = 0;
        private const int STRING_NOT_CONTAINS = 1;
        private const int STRING_STARTS_WITH = 2;
        private const int STRING_MATCHES = 3;

        public void StringFilterAction(int action, IldPropertyInfo property, MenuItem baseItem, MenuItem cancelItem)
        {
            string filterVal = "";

            StringInputDialog sid = new StringInputDialog();
            sid.Title = "Enter String Filter";
            sid.Description = action switch
            {
                0 => "Match items that contain the value entered here:",
                1 => "Match items that do not contain the value entered here:",
                2 => "Match items that start with the value entered here:",
                3 => "Match items that exactly match the value entered here:",
                _ => "Enter the value to filter by:"
            };

            sid.ShowDialog();
            if (sid.DialogResult)
            {
                filterVal = sid.Value;
            }
            else
            {
                return;
            }
        }

        #endregion

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
        }

        private void sli_RequestMoveDown(object? sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void sli_RequestDelete(object? sender, EventArgs e)
        {
            if (sender is SelectableListItem sli)
            {
                selPanel.RemoveItem(sli);

                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void btnDeselect_Click(object sender, RoutedEventArgs e)
        {
            selPanel.DeselectAll();
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
    }
}
