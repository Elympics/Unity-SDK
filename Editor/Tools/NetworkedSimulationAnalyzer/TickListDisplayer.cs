using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Elympics
{
    internal class TickListDisplayer
    {
        private static readonly int TickEntryHeight = 18;

        private readonly TickDataDisplayer _tickDataDisplayer;

        internal TickEntryData LatestTick => AllTicksData.Count != 0 ? AllTicksData[^1] : null;
        internal List<TickEntryData> AllTicksData { get; }
        internal ListView TickListElement { get; }

        internal TickListDisplayer(VisualElement root, VisualTreeAsset tickEntryTemplate, TickDataDisplayer tickDataDisplayer)
        {
            _tickDataDisplayer = tickDataDisplayer;

            // Create data collection to bind
            AllTicksData = new List<TickEntryData>();

            // Store a reference to the list element and initialize it
            TickListElement = root.Q<ListView>("tick-list");
            InitializeTickList(tickEntryTemplate);

            // Register tick selection callback
            TickListElement.onSelectionChange += tickDataDisplayer.OnTickSelected;
        }

        private void InitializeTickList(VisualTreeAsset listEntryTemplate)
        {
            // Ensure proper displaying
            TickListElement.Q<ScrollView>().horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            // Set up a make item function for a list entry
            TickListElement.makeItem = () =>
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
            TickListElement.bindItem = (item, index) => (item.userData as TickEntryDisplayer)?.SetTickEntryData(AllTicksData[index]);

            // Set the actual item's source list/array
            TickListElement.itemsSource = AllTicksData;

            // Set up list properties
            TickListElement.fixedItemHeight = TickEntryHeight;
            TickListElement.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        }

        internal void AddTick(ElympicsSnapshotWithMetadata snapshot)
        {
            AllTicksData.Add(new TickEntryData(snapshot, _tickDataDisplayer.IsBots.Length));

            // Refresh list visual state
            TickListElement.RefreshItems();
        }
    }
}
