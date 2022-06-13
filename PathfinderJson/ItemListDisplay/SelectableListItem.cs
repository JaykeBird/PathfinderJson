using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using SolidShineUi;
using System.Reflection;

namespace PathfinderJson.Ild
{
    public abstract class SelectableListItem : SelectableUserControl
    {

        public event EventHandler? RequestMoveUp;
        public event EventHandler? RequestMoveDown;
        public event EventHandler? RequestDelete;
        public event EventHandler? ContentChanged; // event just to update main window's "isDirty" value

        public abstract void LoadValues(Dictionary<IldPropertyInfo, object> properties);

        private protected static void LoadValuesInternal(SelectableListItem baseObject, Dictionary<IldPropertyInfo, object> properties)
        {
            Type t = baseObject.GetType();
            PropertyInfo[] props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo pi in props)
            {
                IldLinkAttribute? ila = pi.GetCustomAttribute<IldLinkAttribute>();
                if (ila != null)
                {
                    string? propName = ila.BaseName;

                    foreach (IldPropertyInfo item in properties.Keys)
                    {
                        if (item.Name == propName)
                        {
                            pi.SetValue(baseObject, properties[item]);
                        }
                    }
                }
            }
        }

        public abstract object? GetPropertyValue(IldPropertyInfo property);

        private protected static object? GetPropertyValueInternal(SelectableListItem baseObject, string property)
        {
            Type t = baseObject.GetType();
            PropertyInfo[] tpr = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo pi in tpr)
            {
                IldLinkAttribute? ila = pi.GetCustomAttribute<IldLinkAttribute>();
                if (ila != null)
                {
                    string? propName = ila.BaseName;

                    if (propName == property)
                    {
                        return pi.GetValue(baseObject);
                    }
                }
            }

            return null;
        }

        public abstract Dictionary<string, object> GetAllProperties();

        private protected static Dictionary<string, object> GetAllPropertiesInternal(SelectableListItem baseObject)
        {
            Dictionary<string, object> props = new Dictionary<string, object>();

            Type t = baseObject.GetType();
            PropertyInfo[] tpr = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo pi in tpr)
            {
                IldLinkAttribute? ila = pi.GetCustomAttribute<IldLinkAttribute>();
                if (ila != null)
                {
                    string? propName = ila.BaseName;

                    if (!string.IsNullOrEmpty(propName))
                    {
                        props[propName] = pi.GetValue(baseObject) ?? "";
                    }
                }
            }

            return props;
        }

        public void DoRequestDelete()
        {
            RequestDelete?.Invoke(this, EventArgs.Empty);
        }

        public void DoRequestMoveUp()
        {
            RequestMoveUp?.Invoke(this, EventArgs.Empty);
        }

        public void DoRequestMoveDown()
        {
            RequestMoveDown?.Invoke(this, EventArgs.Empty);
        }

        public void DoContentChanged()
        {
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RequestDeleteEventHandler(object sender, RoutedEventArgs e)
        {
            DoRequestDelete();
        }
    }
}
