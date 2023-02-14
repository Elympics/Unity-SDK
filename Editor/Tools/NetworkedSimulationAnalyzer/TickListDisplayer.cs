#if UNITY_2020_2_OR_NEWER
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Elympics
{
    internal class TickListDisplayer
    {
        private static readonly int TickEntryHeight = 18;

        private readonly TickDataDisplayer _tickDataDisplayer;
        private readonly ListView _tickList;
        private readonly List<TickEntryData> _allTicksData;

        internal TickEntryData LatestTick => _allTicksData.Count != 0 ? _allTicksData[_allTicksData.Count - 1] : null;
        internal List<TickEntryData> AllTicksData => _allTicksData;
        internal ListView TickListElement => _tickList;

        internal TickListDisplayer(VisualElement root, VisualTreeAsset tickEntryTemplate, TickDataDisplayer tickDataDisplayer)
        {
            _tickDataDisplayer = tickDataDisplayer;

            // Create data collection to bind
            _allTicksData = new List<TickEntryData>();

            // Store a reference to the list element and initialize it
            _tickList = root.Q<ListView>("tick-list");
            InitializeTickList(tickEntryTemplate);

            // Register tick selection callback
            _tickList.onSelectionChange += tickDataDisplayer.OnTickSelected;
        }

        private void InitializeTickList(VisualTreeAsset listEntryTemplate)
        {
            // Ensure proper displaying
#if UNITY_2021_2_OR_NEWER
            _tickList.Q<ScrollView>().horizontalScrollerVisibility = ScrollerVisibility.Hidden;
#else
            _tickList.Q<ScrollView>().showHorizontal = false;
#endif

            // Set up a make item function for a list entry
            _tickList.makeItem = () =>
            {
                // Instantiate the UXML template for the entry and provide corresponding style
                var newListEntry = listEntryTemplate.CloneTree();
                newListEntry.AddToClassList("tick-entry");

                // Instantiate a controller for the data
                var newListEntryLogic = new TickEntryDisplayer();

                // Assign the controller script to the visual element
                newListEntry.userData = newListEntryLogic;

                // Initialize the controller script
                newListEntryLogic.InitVisualElement(newListEntry);

                // Return the root of the instantiated visual tree
                return newListEntry;
            };

            // Set up bind function for a specific list entry
            _tickList.bindItem = (item, index) =>
            {
                (item.userData as TickEntryDisplayer)?.SetTickEntryData(_allTicksData[index]);
            };

            // Set the actual item's source list/array
            _tickList.itemsSource = _allTicksData;

            // Set up list properties
#if UNITY_2021_2_OR_NEWER
            _tickList.fixedItemHeight = TickEntryHeight;
            _tickList.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
#else
            _tickList.itemHeight = TickEntryHeight;
#endif
        }

        internal void AddTick(ElympicsSnapshotWithMetadata snapshot)
        {
            _allTicksData.Add(new TickEntryData(snapshot, _tickDataDisplayer.IsBots.Length));

            // Refresh list visual state
#if UNITY_2021_2_OR_NEWER
            _tickList.RefreshItems();
#else
            _tickList.Refresh();
#endif
        }
    }
}
#endif
