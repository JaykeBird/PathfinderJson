using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SolidShineUi;

namespace PathfinderJson
{
    /// <summary>
    /// A control that can list a collection of items in a person slot.
    /// </summary>
    public partial class PersonSlotItemList : UserControl
    {
        public PersonSlotItemList()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The color used for this slot, to help users with identifying the slot.
        /// </summary>
        public Color SlotColor { get => (Color)GetValue(SlotColorProperty); set => SetValue(SlotColorProperty, value); }

        /// <summary>The backing dependency property for <see cref="SlotColor"/>. See the related property for details.</summary>
        public static DependencyProperty SlotColorProperty
            = DependencyProperty.Register(nameof(SlotColor), typeof(Color), typeof(PersonSlotItemList),
            new FrameworkPropertyMetadata(Colors.White));

        /// <summary>
        /// The title of the slot.
        /// </summary>
        public string Title { get => (string)GetValue(SlotTitleProperty); set => SetValue(SlotTitleProperty, value); }

        /// <summary>The backing dependency property for <see cref="Title"/>. See the related property for details.</summary>
        public static DependencyProperty SlotTitleProperty
            = DependencyProperty.Register(nameof(Title), typeof(string), typeof(PersonSlotItemList),
            new FrameworkPropertyMetadata("None"));

        /// <summary>
        /// A collection of the editors for the equipment that is currently in this slot.
        /// </summary>
        public SelectableCollection<SmallItemDisplay> EquipmentEditors { get => (SelectableCollection<SmallItemDisplay>)GetValue(EquipmentEditorsProperty); set => SetValue(EquipmentEditorsProperty, value); }

        /// <summary>The backing dependency property for <see cref="EquipmentEditors"/>. See the related property for details.</summary>
        public static DependencyProperty EquipmentEditorsProperty
            = DependencyProperty.Register(nameof(EquipmentEditors), typeof(SelectableCollection<SmallItemDisplay>), typeof(PersonSlotItemList),
            new FrameworkPropertyMetadata(new SelectableCollection<SmallItemDisplay>()));



    }
}
