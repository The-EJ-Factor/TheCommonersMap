using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TheCommonersMap
{
    public partial class MainWindow : Window
    {
        private List<MapNode> currentNodes = new();

        private MapNode pendingAddConnectionNode;
        private MapNode pendingRemoveConnectionNode;
        enum NodeType { Start, Battle, MiniBoss, Chest, Shop, Anvil, Scientist, Campfire, RandomEvent, End }
        Dictionary<NodeType, string> customNodeNames = new Dictionary<NodeType, string>();
        class MapNode
        {
            public NodeType Type;
            public string Name;
            public Point Position;
            public List<MapNode> Children = new List<MapNode>();
            public MapNode Parent;
            public int ParentCount;
            public Button ButtonControl;
            public int Level;
            public string Note { get; set; }

            public bool RandomEventResolved;
            public bool RandomEventRevealed;
            public NodeType RandomEventResolvedType;
            public string RandomEventScenario;

        }


        readonly Dictionary<NodeType, Brush> TypeColors = new Dictionary<NodeType, Brush>()
        {
            { NodeType.Start, Brushes.Cyan },
            { NodeType.Battle, Brushes.LightBlue },
            { NodeType.MiniBoss, Brushes.Orange },
            { NodeType.Chest, Brushes.Gold },
            { NodeType.Shop, Brushes.LightGreen },
            { NodeType.Anvil, Brushes.Violet },
            { NodeType.Scientist, Brushes.Green },
            { NodeType.Campfire, Brushes.LightSalmon },
            { NodeType.RandomEvent, Brushes.White },
            { NodeType.End, Brushes.Red }
        };

        Random rng = new Random();
        int seed = Environment.TickCount;
        
        
        double randomEventSpecialWeight = 1.0;
        double randomEventNodeWeight = 1.0;

        double randomEventBattleWeight = 1.0;
        double randomEventMiniBossWeight = 1.0;
        double randomEventChestWeight = 1.0;
        double randomEventShopWeight = 1.0;
        double randomEventAnvilWeight = 1.0;
        double randomEventScientistWeight = 1.0;
        double randomEventCampfireWeight = 1.0;
        // editable scenario list for Random Event (extend in code)
        class RandomEventScenario
        {
            public string Text;
            public double Weight;

            public RandomEventScenario(string text, double weight)
            {
                Text = text;
                Weight = weight;
            }
        }

        List<RandomEventScenario> randomEventScenarios = new List<RandomEventScenario>()
        {
            new RandomEventScenario("You find a lost traveler who offers a useful item.", 1.0),
            new RandomEventScenario("A trap! Avoid or take damage.", 1.0),
            new RandomEventScenario("An NPC asks for help — choose reward.", 1.0)
        };

        //private void EditRandomEvents()
        //{
        //    Window window = new Window
        //    {
        //        Title = "Random Events",
        //        Width = 650,
        //        Height = 450,
        //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //        Owner = this,
        //        Background = Brushes.Black
        //    };

        //    Grid grid = new Grid
        //    {
        //        Margin = new Thickness(10)
        //    };

        //    grid.ColumnDefinitions.Add(new ColumnDefinition
        //    {
        //        Width = new GridLength(2, GridUnitType.Star)
        //    });

        //    grid.ColumnDefinitions.Add(new ColumnDefinition
        //    {
        //        Width = new GridLength(3, GridUnitType.Star)
        //    });

        //    grid.RowDefinitions.Add(new RowDefinition());
        //    grid.RowDefinitions.Add(new RowDefinition
        //    {
        //        Height = GridLength.Auto
        //    });

        //    ListBox listBox = new ListBox
        //    {
        //        Background = Brushes.Black,
        //        Foreground = Brushes.White,
        //        BorderBrush = Brushes.Gray
        //    };

        //    foreach (string scenario in randomEventScenarios)
        //    {
        //        listBox.Items.Add(scenario);
        //    }

        //    Grid.SetColumn(listBox, 0);
        //    Grid.SetRow(listBox, 0);
        //    grid.Children.Add(listBox);

        //    TextBox editor = new TextBox
        //    {
        //        AcceptsReturn = true,
        //        TextWrapping = TextWrapping.Wrap,
        //        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        //        Background = Brushes.Black,
        //        Foreground = Brushes.White,
        //        BorderBrush = Brushes.Gray,
        //        Margin = new Thickness(10, 0, 0, 0)
        //    };

        //    Grid.SetColumn(editor, 1);
        //    Grid.SetRow(editor, 0);
        //    grid.Children.Add(editor);

        //    listBox.SelectionChanged += (s, e) =>
        //    {
        //        if (listBox.SelectedIndex >= 0)
        //            editor.Text = listBox.SelectedItem.ToString();
        //    };

        //    StackPanel buttons = new StackPanel
        //    {
        //        Orientation = Orientation.Horizontal,
        //        Margin = new Thickness(0, 10, 0, 0)
        //    };

        //    Button addButton = new Button
        //    {
        //        Content = "Add",
        //        Width = 80,
        //        Margin = new Thickness(0, 0, 6, 0)
        //    };

        //    Button updateButton = new Button
        //    {
        //        Content = "Update",
        //        Width = 80,
        //        Margin = new Thickness(0, 0, 6, 0)
        //    };

        //    Button removeButton = new Button
        //    {
        //        Content = "Remove",
        //        Width = 80,
        //        Margin = new Thickness(0, 0, 6, 0)
        //    };

        //    Button closeButton = new Button
        //    {
        //        Content = "Close",
        //        Width = 80,
        //        Margin = new Thickness(20, 0, 0, 0)
        //    };

        //    addButton.Click += (s, e) =>
        //    {
        //        if (string.IsNullOrWhiteSpace(editor.Text))
        //            return;

        //        listBox.Items.Add(editor.Text.Trim());
        //        listBox.SelectedIndex = listBox.Items.Count - 1;
        //        editor.Clear();
        //    };

        //    updateButton.Click += (s, e) =>
        //    {
        //        if (listBox.SelectedIndex < 0)
        //            return;

        //        if (string.IsNullOrWhiteSpace(editor.Text))
        //            return;

        //        int index = listBox.SelectedIndex;
        //        listBox.Items[index] = editor.Text.Trim();
        //        listBox.SelectedIndex = index;
        //    };

        //    removeButton.Click += (s, e) =>
        //    {
        //        if (listBox.SelectedIndex < 0)
        //            return;

        //        int index = listBox.SelectedIndex;
        //        listBox.Items.RemoveAt(index);

        //        if (listBox.Items.Count > 0)
        //            listBox.SelectedIndex = Math.Min(index, listBox.Items.Count - 1);
        //        else
        //            editor.Clear();
        //    };

        //    closeButton.Click += (s, e) =>
        //    {
        //        randomEventScenarios.Clear();

        //        foreach (object item in listBox.Items)
        //        {
        //            randomEventScenarios.Add(item.ToString());
        //        }

        //        window.Close();
        //    };

        //    buttons.Children.Add(addButton);
        //    buttons.Children.Add(updateButton);
        //    buttons.Children.Add(removeButton);
        //    buttons.Children.Add(closeButton);

        //    Grid.SetColumnSpan(buttons, 2);
        //    Grid.SetRow(buttons, 1);
        //    grid.Children.Add(buttons);

        //    window.Content = grid;

        //    window.ShowDialog();
        //}
        private void EditRandomEvents()
        {
            Window window = new Window
            {
                Title = "Random Events",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = Brushes.Black
            };

            Grid grid = new Grid
            {
                Margin = new Thickness(10)
            };

            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            TabControl tabs = new TabControl();

            // ------------------------------------------------------------
            // TAB 1: GENERAL WEIGHTS
            // ------------------------------------------------------------

            TabItem generalTab = new TabItem
            {
                Header = "Event Odds"
            };

            StackPanel generalPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            generalPanel.Children.Add(new TextBlock
            {
                Text = "What happens when a Random Event is revealed?",
                Foreground = Brushes.White,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 12)
            });

            TextBox specialWeightBox = CreateWeightBox(
                randomEventSpecialWeight);

            TextBox nodeWeightBox = CreateWeightBox(
                randomEventNodeWeight);

            generalPanel.Children.Add(
                CreateWeightRow(
                    "Special Event",
                    specialWeightBox));

            generalPanel.Children.Add(
                CreateWeightRow(
                    "Resolve into Node",
                    nodeWeightBox));

            generalPanel.Children.Add(new TextBlock
            {
                Text =
                    "These are relative weights. For example, 3 vs 1 means\n" +
                    "Special Events are three times as likely as Node resolutions.",
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 12, 0, 0)
            });

            generalTab.Content = generalPanel;

            // ------------------------------------------------------------
            // TAB 2: NODE RESOLUTION
            // ------------------------------------------------------------

            TabItem nodeTab = new TabItem
            {
                Header = "Node Resolution"
            };

            StackPanel nodePanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            nodePanel.Children.Add(new TextBlock
            {
                Text = "Weights used when a Random Event resolves into a node",
                Foreground = Brushes.White,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 12)
            });

            Dictionary<NodeType, TextBox> nodeWeightBoxes =
                new Dictionary<NodeType, TextBox>();

            AddRandomNodeWeightRow(
                nodePanel,
                nodeWeightBoxes,
                NodeType.Battle,
                "Battle",
                randomEventBattleWeight);

            AddRandomNodeWeightRow(
                nodePanel,
                nodeWeightBoxes,
                NodeType.MiniBoss,
                "Mini Boss",
                randomEventMiniBossWeight);

            AddRandomNodeWeightRow(
                nodePanel,
                nodeWeightBoxes,
                NodeType.Chest,
                "Chest",
                randomEventChestWeight);

            AddRandomNodeWeightRow(
                nodePanel,
                nodeWeightBoxes,
                NodeType.Shop,
                "Shop",
                randomEventShopWeight);

            AddRandomNodeWeightRow(
                nodePanel,
                nodeWeightBoxes,
                NodeType.Anvil,
                "Anvil",
                randomEventAnvilWeight);

            AddRandomNodeWeightRow(
                nodePanel,
                nodeWeightBoxes,
                NodeType.Scientist,
                "Scientist",
                randomEventScientistWeight);

            AddRandomNodeWeightRow(
                nodePanel,
                nodeWeightBoxes,
                NodeType.Campfire,
                "Campfire",
                randomEventCampfireWeight);

            nodePanel.Children.Add(new TextBlock
            {
                Text =
                    "These weights are independent from the normal map weights.",
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 12, 0, 0)
            });

            nodeTab.Content = nodePanel;

            // ------------------------------------------------------------
            // TAB 3: SPECIAL EVENTS
            // ------------------------------------------------------------

            TabItem eventsTab = new TabItem
            {
                Header = "Special Events"
            };

            Grid eventsGrid = new Grid
            {
                Margin = new Thickness(10)
            };

            eventsGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(2, GridUnitType.Star)
                });

            eventsGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(3, GridUnitType.Star)
                });

            eventsGrid.RowDefinitions.Add(
                new RowDefinition());

            eventsGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                });

            ListBox listBox = new ListBox
            {
                Background = Brushes.Black,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray
            };

            foreach (RandomEventScenario scenario in randomEventScenarios)
            {
                listBox.Items.Add(scenario);
            }

            listBox.DisplayMemberPath = "Text";

            Grid.SetColumn(listBox, 0);
            Grid.SetRow(listBox, 0);
            eventsGrid.Children.Add(listBox);

            Grid editorGrid = new Grid
            {
                Margin = new Thickness(10, 0, 0, 0)
            };

            editorGrid.RowDefinitions.Add(new RowDefinition());
            editorGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            editorGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            TextBox editor = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.Black,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray
            };

            Grid.SetRow(editor, 0);
            editorGrid.Children.Add(editor);

            StackPanel weightPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 8)
            };

            weightPanel.Children.Add(new TextBlock
            {
                Text = "Weight:",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });

            TextBox eventWeightBox = CreateWeightBox(1.0);
            eventWeightBox.Width = 100;

            weightPanel.Children.Add(eventWeightBox);

            Grid.SetRow(weightPanel, 1);
            editorGrid.Children.Add(weightPanel);

            Grid.SetRow(editorGrid, 0);
            Grid.SetColumn(editorGrid, 1);
            eventsGrid.Children.Add(editorGrid);

            listBox.SelectionChanged += (s, e) =>
            {
                RandomEventScenario selected =
                    listBox.SelectedItem as RandomEventScenario;

                if (selected == null)
                    return;

                editor.Text = selected.Text;
                eventWeightBox.Text = selected.Weight.ToString();
            };

            StackPanel eventButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button addButton = new Button
            {
                Content = "Add",
                Width = 80,
                Margin = new Thickness(0, 0, 6, 0)
            };

            Button updateButton = new Button
            {
                Content = "Update",
                Width = 80,
                Margin = new Thickness(0, 0, 6, 0)
            };

            Button removeButton = new Button
            {
                Content = "Remove",
                Width = 80,
                Margin = new Thickness(0, 0, 6, 0)
            };

            addButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(editor.Text))
                    return;

                double weight;

                if (!double.TryParse(
                    eventWeightBox.Text,
                    out weight))
                {
                    MessageBox.Show(
                        "Weight must be a number.",
                        "Invalid Weight",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (weight < 0)
                {
                    MessageBox.Show(
                        "Weight cannot be negative.",
                        "Invalid Weight",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                RandomEventScenario scenario =
                    new RandomEventScenario(
                        editor.Text.Trim(),
                        weight);

                listBox.Items.Add(scenario);
                listBox.SelectedItem = scenario;

                editor.Clear();
                eventWeightBox.Text = "1";
            };

            updateButton.Click += (s, e) =>
            {
                RandomEventScenario selected =
                    listBox.SelectedItem as RandomEventScenario;

                if (selected == null)
                    return;

                if (string.IsNullOrWhiteSpace(editor.Text))
                    return;

                double weight;

                if (!double.TryParse(
                    eventWeightBox.Text,
                    out weight))
                {
                    MessageBox.Show(
                        "Weight must be a number.",
                        "Invalid Weight",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (weight < 0)
                {
                    MessageBox.Show(
                        "Weight cannot be negative.",
                        "Invalid Weight",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                selected.Text = editor.Text.Trim();
                selected.Weight = weight;

                listBox.Items.Refresh();
            };

            removeButton.Click += (s, e) =>
            {
                if (listBox.SelectedIndex < 0)
                    return;

                listBox.Items.RemoveAt(
                    listBox.SelectedIndex);

                editor.Clear();
                eventWeightBox.Text = "1";
            };

            eventButtons.Children.Add(addButton);
            eventButtons.Children.Add(updateButton);
            eventButtons.Children.Add(removeButton);

            Grid.SetRow(eventButtons, 1);
            Grid.SetColumnSpan(eventButtons, 2);
            eventsGrid.Children.Add(eventButtons);

            eventsTab.Content = eventsGrid;

            tabs.Items.Add(generalTab);
            tabs.Items.Add(nodeTab);
            tabs.Items.Add(eventsTab);

            Grid.SetRow(tabs, 0);
            grid.Children.Add(tabs);

            // ------------------------------------------------------------
            // SAVE / CANCEL
            // ------------------------------------------------------------

            StackPanel bottomButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button saveButton = new Button
            {
                Content = "Save",
                Width = 80,
                Margin = new Thickness(0, 0, 6, 0)
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80
            };

            saveButton.Click += (s, e) =>
            {
                double value;

                if (!double.TryParse(
                    specialWeightBox.Text,
                    out value) || value < 0)
                {
                    MessageBox.Show(
                        "Special Event weight must be a non-negative number.");

                    return;
                }

                randomEventSpecialWeight = value;

                if (!double.TryParse(
                    nodeWeightBox.Text,
                    out value) || value < 0)
                {
                    MessageBox.Show(
                        "Node Resolution weight must be a non-negative number.");

                    return;
                }

                randomEventNodeWeight = value;

                randomEventBattleWeight =
                    GetNodeWeight(nodeWeightBoxes, NodeType.Battle);

                randomEventMiniBossWeight =
                    GetNodeWeight(nodeWeightBoxes, NodeType.MiniBoss);

                randomEventChestWeight =
                    GetNodeWeight(nodeWeightBoxes, NodeType.Chest);

                randomEventShopWeight =
                    GetNodeWeight(nodeWeightBoxes, NodeType.Shop);

                randomEventAnvilWeight =
                    GetNodeWeight(nodeWeightBoxes, NodeType.Anvil);

                randomEventScientistWeight =
                    GetNodeWeight(nodeWeightBoxes, NodeType.Scientist);

                randomEventCampfireWeight =
                    GetNodeWeight(nodeWeightBoxes, NodeType.Campfire);

                randomEventScenarios.Clear();

                foreach (object item in listBox.Items)
                {
                    RandomEventScenario scenario =
                        item as RandomEventScenario;

                    if (scenario != null)
                        randomEventScenarios.Add(scenario);
                }

                window.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                window.Close();
            };

            bottomButtons.Children.Add(saveButton);
            bottomButtons.Children.Add(cancelButton);

            Grid.SetRow(bottomButtons, 1);
            grid.Children.Add(bottomButtons);

            window.Content = grid;

            window.ShowDialog();
        }
        private TextBox CreateWeightBox(double value)
        {
            return new TextBox
            {
                Text = value.ToString(),
                Width = 100,
                Background = Brushes.Black,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray
            };
        }
        private StackPanel CreateWeightRow(string name, TextBox weightBox)
        {
            StackPanel row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 4)
            };

            row.Children.Add(new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                Width = 180,
                VerticalAlignment = VerticalAlignment.Center
            });

            row.Children.Add(weightBox);

            return row;
        }

        private void AddRandomNodeWeightRow(
        StackPanel panel,
        Dictionary<NodeType, TextBox> boxes,
        NodeType type,
        string name,
        double weight)
        {
            TextBox box = CreateWeightBox(weight);

            boxes[type] = box;

            panel.Children.Add(
                CreateWeightRow(name, box));
        }

        private double GetNodeWeight(
            Dictionary<NodeType, TextBox> boxes,
            NodeType type)
        {
            double value;

            if (!double.TryParse(boxes[type].Text, out value))
            {
                MessageBox.Show(
                    "All node weights must be numbers.",
                    "Invalid Weight",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return 0;
            }

            return Math.Max(0, value);
        }
        public MainWindow()
        {
            InitializeComponent();
            DepthLabel.Text = DepthSlider.Value.ToString();
            MaxBranchLabel.Text = MaxBranchSlider.Value.ToString();
            Loaded += (s, e) => UpdateWeightLabels();
            DepthSlider.ValueChanged += (s, e) => DepthLabel.Text = DepthSlider.Value.ToString();
            MaxBranchSlider.ValueChanged += (s, e) => MaxBranchLabel.Text = MaxBranchSlider.Value.ToString();

            Loaded += (s, e) =>
            {
                ZoomSlider.ValueChanged += (s2, e2) => ApplyZoom();
                ApplyNormal_Click(null, null);
                RandomizeSeedBtn_Click(null, null);
                GenerateMap();
            };
        }

        private void WeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateWeightLabels();
        }

        void UpdateWeightLabels()
        {
            var pairs = new (Slider slider, TextBlock label)[]
            {
        (WeightBattle, WeightBattleLabel),
        (WeightMiniBoss, WeightMiniBossLabel),
        (WeightChest, WeightChestLabel),
        (WeightShop, WeightShopLabel),
        (WeightAnvil, WeightAnvilLabel),
        (WeightScientist, WeightScientistLabel),
        (WeightCampfire, WeightCampfireLabel),
        (WeightRandomEvent, WeightRandomEventLabel)
            };

            foreach (var (slider, label) in pairs)
            {
                if (slider == null || label == null) continue;
                label.Text = ((int)slider.Value).ToString();
            }
        }


        private void RandomizeSeedBtn_Click(object sender, RoutedEventArgs e)
        {
            seed = Environment.TickCount ^ (int)DateTime.Now.Ticks;
            SeedBox.Text = seed.ToString();
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SeedBox.Text.Trim().ToLower() == "random")
            {
                seed = Environment.TickCount ^ (int)DateTime.Now.Ticks;
            }
            else
            {
                if (!int.TryParse(SeedBox.Text, out seed))
                {
                    // hash the string deterministically
                    seed = SeedBox.Text.Aggregate(0, (acc, ch) => acc * 31 + ch);
                }
            }
            rng = new Random(seed);
            GenerateMap();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (MapCanvas == null) return; // Prevent crash
            double s = ZoomSlider.Value;
            MapCanvas.LayoutTransform = new ScaleTransform(s, s);
        }

        private void ApplyEasy_Click(object sender, RoutedEventArgs e)
        {
            WeightBattle.Value = 3;
            WeightMiniBoss.Value = 1;
            WeightChest.Value = 1;
            WeightShop.Value = 1;
            WeightAnvil.Value = 1;
            WeightScientist.Value = 1;
            WeightCampfire.Value = 1;
            WeightRandomEvent.Value = 1;
        }

        private void ApplyNormal_Click(object sender, RoutedEventArgs e)
        {
            WeightBattle.Value = 4;
            WeightMiniBoss.Value = 2;
            WeightChest.Value = 1;
            WeightShop.Value = 1;
            WeightAnvil.Value = 1;
            WeightScientist.Value = 1;
            WeightCampfire.Value = 1;
            WeightRandomEvent.Value = 1;
        }

        private void ApplyHard_Click(object sender, RoutedEventArgs e)
        {
            WeightBattle.Value = 4;
            WeightMiniBoss.Value = 3;
            WeightChest.Value = 1;
            WeightShop.Value = 1;
            WeightAnvil.Value = 1;
            WeightScientist.Value = 1;
            WeightCampfire.Value = 1;
            WeightRandomEvent.Value = 1;
        }

        private void GenerateMap()
        {
            // clear canvas
            MapCanvas.Children.Clear();

            // setup RNG from seed box

            int maxNodesPerLevel = (int)MaxNodesPerLevelSlider.Value;


            if (SeedBox.Text.Trim().ToLower() == "random")
            {
                seed = Environment.TickCount ^ (int)DateTime.Now.Ticks;
            }
            else
            {
                if (!int.TryParse(SeedBox.Text, out seed))
                {
                    seed = SeedBox.Text.Aggregate(0, (acc, ch) => acc * 31 + ch);
                }
            }
            rng = new Random(seed);

            int depth = (int)DepthSlider.Value;
            int maxBranches = (int)MaxBranchSlider.Value;

            // assemble weight table (ignore Start & End)
            var weights = new Dictionary<NodeType, int>
            {
                { NodeType.Battle, (int)WeightBattle.Value },
                { NodeType.MiniBoss, (int)WeightMiniBoss.Value },
                { NodeType.Chest, (int)WeightChest.Value },
                { NodeType.Shop, (int)WeightShop.Value },
                { NodeType.Anvil, (int)WeightAnvil.Value },
                { NodeType.Scientist, (int)WeightScientist.Value },
                { NodeType.Campfire, (int)WeightCampfire.Value },
                { NodeType.RandomEvent, (int)WeightRandomEvent.Value }
            };

            // Build level-by-level nodes (levels 0..depth-1), End at level depth
            var levels = new List<List<MapNode>>();
            for (int i = 0; i <= depth - 1; i++) levels.Add(new List<MapNode>());


            // create start node at level 0
            var start = new MapNode() { Type = NodeType.Start, Level = 0 };
            levels[0].Add(start);

            // create nodes level-by-level until last-1
            // Level 0 is the real start
            levels[0] = new List<MapNode> { new MapNode { Level = 0 } };

            double linearity = LinearitySlider.Value; // 0 = chaotic, 1 = linear

            // --- LEVEL-BY-LEVEL NODES ---
            for (int lvl = 1; lvl <= depth - 1; lvl++)
            {
                var prevLevel = levels[lvl - 1];
                var thisLevelList = new List<MapNode>();

                // --- Compute min/max nodes for this level based on next layer ---
                int minNodes = (int)Math.Ceiling(prevLevel.Count / (double)maxBranches);
                int maxNodes = Math.Min(prevLevel.Count * maxBranches, maxNodesPerLevel);

                // Apply layer size constraints relative to the next layer (prevent boss overload)
                if (lvl == depth - 2) // third-to-last layer (above boss)
                {
                    minNodes = Math.Max(minNodes, prevLevel.Count); // ensure every parent can have at least 1 child
                }
                if (lvl == depth - 1) // layer directly above boss
                {
                    maxNodes = Math.Min(maxNodes, maxBranches); // boss cannot have more than maxBranches parents
                }

                // Safety check
                minNodes = Math.Min(minNodes, maxNodes);

                // Pick actual node count
                int nextLevelCount = rng.Next(minNodes, maxNodes + 1);

                // Create nodes with weighted types
                for (int i = 0; i < nextLevelCount; i++)
                {
                    var node = new MapNode { Level = lvl };
                    node.Type = PickNodeTypeByWeight(weights);
                    thisLevelList.Add(node);
                }

                // Track child counts per parent and parent counts per child
                var childCountMap = prevLevel.ToDictionary(p => p, p => p.Children.Count);
                var parentCountMap = thisLevelList.ToDictionary(n => n, n => 0);
                int maxParentsPerNode = maxBranches;

                // --- Ensure every child has at least 1 parent ---
                foreach (var child in thisLevelList)
                {
                    var availableParents = prevLevel.Where(p => childCountMap[p] < maxBranches).ToList();
                    if (availableParents.Count == 0)
                        availableParents = prevLevel.ToList();

                    // Proximity bias based on linearity
                    var weightedParents = availableParents
                        .Select(p => new
                        {
                            Parent = p,
                            Distance = Math.Abs(prevLevel.IndexOf(p) - thisLevelList.IndexOf(child))
                        })
                        .OrderBy(x =>
                        {
                            double randomFactor = rng.NextDouble() * (1 - linearity); // randomness decreases as linearity increases
                            return (x.Distance * linearity) + randomFactor;
                        })
                        .Select(x => x.Parent)
                        .ToList();

                    var parent = weightedParents.First();
                    parent.Children.Add(child);
                    child.Parent = parent;
                    childCountMap[parent]++;
                    parentCountMap[child]++;
                }

                // --- Ensure every parent has at least 1 child ---
                foreach (var parent in prevLevel)
                {
                    while (childCountMap[parent] < 1)
                    {
                        var weightedChildren = thisLevelList
                            .Where(c => parentCountMap[c] < maxBranches)
                            .Select(c => new
                            {
                                Child = c,
                                Distance = Math.Abs(prevLevel.IndexOf(parent) - thisLevelList.IndexOf(c))
                            })
                            .OrderBy(x =>
                            {
                                double randomFactor = rng.NextDouble() * (1 - linearity);
                                return (x.Distance * linearity) + randomFactor;
                            })
                            .Select(x => x.Child)
                            .ToList();

                        if (weightedChildren.Count == 0)
                            break;

                        var child = weightedChildren.First();
                        if (!parent.Children.Contains(child))
                        {
                            parent.Children.Add(child);
                            child.Parent = child.Parent ?? parent;
                            childCountMap[parent]++;
                            parentCountMap[child]++;
                        }
                    }
                }



                levels[lvl] = thisLevelList;
            }

            // --- End ---
            var End = new MapNode { Level = depth, Type = NodeType.End };
            levels.Add(new List<MapNode> { End });

            // Track how many parents the boss has
            int bossParentCount = 0;

            foreach (var parent in levels[depth - 1])
            {
                if (parent.Children.Count < maxBranches && bossParentCount < maxBranches)
                {
                    parent.Children.Add(End);
                    if (End.Parent == null)
                        End.Parent = parent;
                    bossParentCount++;
                }
            }

            // Ensure the boss has at least 1 parent
            if (End.Parent == null && levels[depth - 1].Count > 0)
            {
                End.Parent = levels[depth - 1][0];
                levels[depth - 1][0].Children.Add(End);
            }



            // Add End as its own single level (we will use levelsCount = depth+1 for layout)
            var allLevels = new List<List<MapNode>>(levels);
            allLevels.Add(new List<MapNode> { End });

            // layout: compute width per level and set canvas size
            double horizontalSpacing = 160; // base spacing
            double verticalSpacing = 140;
            int widest = allLevels.Max(l => l.Count);
            double canvasWidth = Math.Max(800, widest * horizontalSpacing + 200);
            double canvasHeight = (allLevels.Count + 1) * verticalSpacing + 200;

            MapCanvas.Width = canvasWidth;
            MapCanvas.Height = canvasHeight;
            MapCanvas.Background = Brushes.Black;

            // position nodes in each deapth centered horizontally
            for (int lvl = 0; lvl < allLevels.Count; lvl++)
            {
                var list = allLevels[lvl];
                double y = 50 + lvl * verticalSpacing;
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    double x;
                    if (count == 1)
                        x = canvasWidth / 2;
                    else
                        x = (canvasWidth / (count + 1)) * (i + 1);
                    list[i].Position = new Point(x, y);
                }
            }

            // draw lines (parents -> children)
            var nodes = allLevels.SelectMany(l => l).ToList();

            // draw connections
            foreach (var node in nodes)
            {
                foreach (var child in node.Children)
                {
                    DrawConnection(node.Position, child.Position);
                }
            }

            // add buttons
            foreach (var node in nodes)
            {
                AddNodeButton(node);
            }
            currentNodes = nodes;
        }

        NodeType PickNodeTypeByWeight(Dictionary<NodeType, int> weights)
        {
            var pickList = new List<(NodeType type, int weight)>();
            foreach (var kv in weights)
            {
                if (kv.Value > 0) pickList.Add((kv.Key, kv.Value));
            }
            if (pickList.Count == 0) return NodeType.Battle;
            int total = pickList.Sum(p => p.weight);
            int r = rng.Next(total);
            int acc = 0;
            foreach (var p in pickList)
            {
                acc += p.weight;
                if (r < acc) return p.type;
            }
            return pickList[0].type;
        }
        private T PickByDoubleWeight<T>(Dictionary<T, double> weights)
        {
            List<KeyValuePair<T, double>> valid =
                weights.Where(x => x.Value > 0).ToList();

            if (valid.Count == 0)
                return default(T);

            double total = valid.Sum(x => x.Value);
            double roll = rng.NextDouble() * total;

            double accumulated = 0;

            foreach (KeyValuePair<T, double> item in valid)
            {
                accumulated += item.Value;

                if (roll < accumulated)
                    return item.Key;
            }

            return valid[valid.Count - 1].Key;
        }


        void DrawConnection(Point a, Point b)
        {
            var line = new Line
            {
                X1 = a.X,
                Y1 = a.Y + 18,
                X2 = b.X,
                Y2 = b.Y - 18,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Opacity = 0.9
            };
            MapCanvas.Children.Add(line);
        }

        void AddNodeButton(MapNode node)
        {
            var btn = new Button
            {
                Width = 120,
                Height = 36,
                Tag = node,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // style: transparent background so canvas black shows through, border subtle
            btn.Background = Brushes.Transparent;
            btn.BorderBrush = Brushes.Gray;
            btn.BorderThickness = new Thickness(1);
            btn.Padding = new Thickness(4, 2, 4, 2);

            string label = NodeLabel(node);
            btn.Content = label;

            // set text color depending on type
            btn.Foreground = TypeColors.ContainsKey(node.Type) ? TypeColors[node.Type] : Brushes.White;

            Canvas.SetLeft(btn, node.Position.X - btn.Width / 2);
            Canvas.SetTop(btn, node.Position.Y - btn.Height / 2);

            btn.Click += NodeButton_Click;
            btn.ContextMenu = BuildNodeContextMenu(node);
            node.ButtonControl = btn;

            // special visual for Start and End
            if (node.Type == NodeType.Start)
            {
                btn.BorderBrush = Brushes.Cyan;
                btn.FontWeight = FontWeights.Bold;
                btn.Width = 140;
            }
            else if (node.Type == NodeType.End)
            {
                btn.BorderBrush = Brushes.Red;
                btn.Width = 160;
                btn.FontWeight = FontWeights.Bold;
            }
            else if (node.Type == NodeType.RandomEvent)
            {
                btn.Content = NodeLabel(node);
                btn.ToolTip = "Random Event (click to resolve)";
            }

            MapCanvas.Children.Add(btn);
        }

        string NodeLabel(MapNode node)
        {
            if (node.Type == NodeType.RandomEvent && node.RandomEventResolved && node.RandomEventRevealed)
            {
                return GetTypeDisplayName(node.RandomEventResolvedType) + "?";
            }

            if (!string.IsNullOrWhiteSpace(node.Name))
            {
                return node.Name;
            }
            
            string customName;

            if (customNodeNames.TryGetValue(node.Type, out customName) &&
                !string.IsNullOrWhiteSpace(customName))
            {
                return customName;
            }

            switch (node.Type)
            {
                case NodeType.Start:
                    return "Start";

                case NodeType.Battle:
                    return "Battle";

                case NodeType.MiniBoss:
                    return "Mini Boss";

                case NodeType.Chest:
                    return "Chest";

                case NodeType.Shop:
                    return "Shop";

                case NodeType.Anvil:
                    return "Anvil";

                case NodeType.Scientist:
                    return "Scientist";

                case NodeType.Campfire:
                    return "Campfire";

                case NodeType.RandomEvent:
                    return "?";

                case NodeType.End:
                    return "End";

                default:
                    return node.Type.ToString();
            }
        }

        private void NodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button b)) return;
            var node = b.Tag as MapNode;
            if (pendingAddConnectionNode != null)
            {
                FinishAddConnection(node);
                return;
            }

            if (pendingRemoveConnectionNode != null)
            {
                FinishRemoveConnection(node);
                return;
            }
            if (node == null) return;

            //switch (node.Type)
            //{
            //    case NodeType.RandomEvent:
            //        ResolveRandomEvent(node, b);
            //        break;
            //case NodeType.Battle:
            //case NodeType.MiniBoss:
            //case NodeType.Chest:
            //case NodeType.Shop:
            //case NodeType.Anvil:
            //case NodeType.Scientist:
            //case NodeType.Campfire:
            //    MessageBox.Show($"Node: {NodeLabel(node)}\n(Level {node.Level})", "Node Info", MessageBoxButton.OK, MessageBoxImage.Information);
            //    break;
            //case NodeType.Start:
            //    MessageBox.Show("Start node. Prepare for the journey!", "Start", MessageBoxButton.OK, MessageBoxImage.Information);
            //    break;
            //case NodeType.End:
            //    MessageBox.Show("End!\nThis ends the run.", "End", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    break;
            //}
            //if (node.RandomEventResolved && node.Type != NodeType.RandomEvent)
            //{
            //    NodeType newType = PickNodeTypeByWeight(weights);

            //    node.RandomEventResolved = true;
            //    node.RandomEventResolvedType = newType;
            //    node.RandomEventScenario = null;

            //    RefreshMap();

            //    ShowRandomEventMessage(
            //        node,
            //        "Random Event: " + GetTypeDisplayName(newType));
            //}

            if (node.Type == NodeType.RandomEvent)
            {
                ResolveRandomEvent(node);
                return;
            }
            ShowNodeNote(node);
        }

        //private void ResolveRandomEvent(MapNode node, Button button)
        //{
        //    int choice = rng.Next(100);
        //    if (choice < 30)
        //    {
        //        // convert into one of the other types (weighted by same weights)
        //        var weights = new Dictionary<NodeType, int>
        //        {
        //            { NodeType.Battle, (int)WeightBattle.Value },
        //            { NodeType.MiniBoss, (int)WeightMiniBoss.Value },
        //            { NodeType.Chest, (int)WeightChest.Value },
        //            { NodeType.Shop, (int)WeightShop.Value },
        //            { NodeType.Anvil, (int)WeightAnvil.Value },
        //            { NodeType.Scientist, (int)WeightScientist.Value },
        //            { NodeType.Campfire, (int)WeightCampfire.Value },
        //            { NodeType.RandomEvent, (int)WeightRandomEvent.Value } // could convert to another random event
        //        };
        //        var newType = PickNodeTypeByWeight(weights);
        //        node.Type = newType;

        //        // update visual
        //        button.Foreground = TypeColors[newType];
        //        button.Content = NodeLabel(node);
        //        button.ToolTip = null;
        //        MessageBox.Show($"Random Event resolved into: {NodeLabel(node)}", "Random Event", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //    else
        //    {
        //        // Show a random scenario from list
        //        int i = rng.Next(randomEventScenarios.Count);
        //        MessageBox.Show(randomEventScenarios[i], "Random Event", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //} 
        // This ? has already rolled an event.
        // Always give the same result.
        //if (node.RandomEventResolved)
        //{
        //    if (node.RandomEventResolvedType == NodeType.RandomEvent)
        //    {
        //        ShowRandomEventMessage(
        //            node,
        //            node.RandomEventScenario);
        //    }
        //    else
        //    {
        //        string result =
        //            "Random Event resolved into: " +
        //            NodeLabel(new MapNode
        //            {
        //                Type = node.RandomEventResolvedType
        //            });

        //        ShowRandomEventMessage(node, result);
        //    }

        //    return;
        //}
        private void ResolveRandomEvent(MapNode node)
        {
            // Already rolled: just reveal/hide the same result.
            if (node.RandomEventResolved)
            {
                node.RandomEventRevealed = !node.RandomEventRevealed;

                RefreshMap();

                if (node.RandomEventRevealed)
                {
                    if (!string.IsNullOrWhiteSpace(node.RandomEventScenario))
                    {
                        ShowRandomEventMessage(
                            node,
                            node.RandomEventScenario);
                    }
                    else
                    {
                        ShowRandomEventMessage(
                            node,
                            "Random Event resolved into: " +
                            GetTypeDisplayName(node.RandomEventResolvedType));
                    }
                }

                return;
            }

            // Decide whether this is a special event or a node resolution.
            Dictionary<string, double> categoryWeights =
                new Dictionary<string, double>
                {
            { "Special", randomEventSpecialWeight },
            { "Node", randomEventNodeWeight }
                };

            string category = PickByDoubleWeight(categoryWeights);

            if (category == "Node")
            {
                Dictionary<NodeType, double> nodeWeights =
                    new Dictionary<NodeType, double>
                    {
                { NodeType.Battle, randomEventBattleWeight },
                { NodeType.MiniBoss, randomEventMiniBossWeight },
                { NodeType.Chest, randomEventChestWeight },
                { NodeType.Shop, randomEventShopWeight },
                { NodeType.Anvil, randomEventAnvilWeight },
                { NodeType.Scientist, randomEventScientistWeight },
                { NodeType.Campfire, randomEventCampfireWeight }
                    };

                NodeType newType = PickByDoubleWeight(nodeWeights);

                node.RandomEventResolved = true;
                node.RandomEventRevealed = true;
                node.RandomEventResolvedType = newType;
                node.RandomEventScenario = null;

                RefreshMap();

                ShowRandomEventMessage(
                    node,
                    "Random Event resolved into: " +
                    GetTypeDisplayName(newType));
            }
            else
            {
                RandomEventScenario scenario =
                    PickRandomEventScenario();

                if (scenario == null)
                {
                    // No custom events exist, so fall back to a node resolution.
                    Dictionary<NodeType, double> nodeWeights =
                        new Dictionary<NodeType, double>
                        {
                    { NodeType.Battle, randomEventBattleWeight },
                    { NodeType.MiniBoss, randomEventMiniBossWeight },
                    { NodeType.Chest, randomEventChestWeight },
                    { NodeType.Shop, randomEventShopWeight },
                    { NodeType.Anvil, randomEventAnvilWeight },
                    { NodeType.Scientist, randomEventScientistWeight },
                    { NodeType.Campfire, randomEventCampfireWeight }
                        };

                    NodeType newType = PickByDoubleWeight(nodeWeights);

                    node.RandomEventResolved = true;
                    node.RandomEventRevealed = true;
                    node.RandomEventResolvedType = newType;
                    node.RandomEventScenario = null;

                    RefreshMap();

                    ShowRandomEventMessage(
                        node,
                        "Random Event resolved into: " +
                        GetTypeDisplayName(newType));

                    return;
                }

                node.RandomEventResolved = true;
                node.RandomEventRevealed = true;
                node.RandomEventResolvedType = NodeType.RandomEvent;
                node.RandomEventScenario = scenario.Text;

                RefreshMap();

                ShowRandomEventMessage(
                    node,
                    scenario.Text);
            }
        }
        private RandomEventScenario PickRandomEventScenario()
        {
            List<RandomEventScenario> valid =
                randomEventScenarios
                    .Where(x => x.Weight > 0)
                    .ToList();

            if (valid.Count == 0)
                return null;

            double total = valid.Sum(x => x.Weight);
            double roll = rng.NextDouble() * total;

            double accumulated = 0;

            foreach (RandomEventScenario scenario in valid)
            {
                accumulated += scenario.Weight;

                if (roll < accumulated)
                    return scenario;
            }

            return valid[valid.Count - 1];
        }
        private void ShowRandomEventMessage(MapNode node, string eventText)
        {
            string text = eventText;

            if (!string.IsNullOrWhiteSpace(node.Note))
            {
                text += "\n\n" + node.Note;
            }

            MessageBox.Show(
                text,
                "Random Event",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        private ContextMenu BuildNodeContextMenu(MapNode node)
        {
            ContextMenu menu = new ContextMenu();

            AddTypeMenuItem(menu, node, NodeType.Battle);
            AddTypeMenuItem(menu, node, NodeType.MiniBoss);
            AddTypeMenuItem(menu, node, NodeType.Chest);
            AddTypeMenuItem(menu, node, NodeType.Shop);
            AddTypeMenuItem(menu, node, NodeType.Anvil);
            AddTypeMenuItem(menu, node, NodeType.Scientist);
            AddTypeMenuItem(menu, node, NodeType.Campfire);
            AddTypeMenuItem(menu, node, NodeType.RandomEvent);

            menu.Items.Add(new Separator());

            MenuItem renameItem = new MenuItem();
            renameItem.Header = "Rename " + NodeLabel(node);

            renameItem.Click += delegate
            {
                if (node.Type == NodeType.End)
                {
                    RenameNode(node);
                }
                else
                {
                    RenameNodeType(node.Type);
                }
            };

            menu.Items.Add(renameItem);

            MenuItem noteItem = new MenuItem();
            noteItem.Header = string.IsNullOrWhiteSpace(node.Note)
                ? "Add Note"
                : "Change Note";

            noteItem.Click += delegate
            {
                EditNodeNote(node);
            };

            menu.Items.Add(noteItem);

            menu.Items.Add(new Separator());

            MenuItem addConnection = new MenuItem
            {
                Header = "Add Connection"
            };

            addConnection.Click += (s, e) =>
            {
                BeginAddConnection(node);
            };

            menu.Items.Add(addConnection);

            MenuItem removeConnection = new MenuItem
            {
                Header = "Remove Connection"
            };

            removeConnection.Click += (s, e) =>
            {
                BeginRemoveConnection(node);
            };

            menu.Items.Add(removeConnection);

            menu.Items.Add(new Separator());

            MenuItem deleteNode = new MenuItem
            {
                Header = "Delete Node"
            };

            deleteNode.Click += (s, e) =>
            {
                DeleteNode(node);
            };

            menu.Items.Add(deleteNode);

            return menu;
        }
        private void AddTypeMenuItem(
        ContextMenu menu,
        MapNode node,
        NodeType type)
        {
            MenuItem item = new MenuItem
            {
                Header = GetTypeDisplayName(type)
            };

            item.Click += (s, e) =>
            {
                if (node.Type == NodeType.Start)
                {
                    MessageBox.Show("Start cannot be replaced");
                    return;
                }

                if (node.Type == NodeType.End)
                {
                    MessageBox.Show("End cannot be replaced");
                    return;
                }

                node.Type = type;

                // Changing a node type always creates a fresh state.
                node.RandomEventResolved = false;
                node.RandomEventRevealed = false;
                node.RandomEventResolvedType = NodeType.RandomEvent;
                node.RandomEventScenario = null;

                RefreshMap();
            };

            menu.Items.Add(item);
        }
        private string GetTypeDisplayName(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return "Battle";
                case NodeType.MiniBoss: return "Mini Boss";
                case NodeType.Chest: return "Chest";
                case NodeType.Shop: return "Shop";
                case NodeType.Anvil: return "Anvil";
                case NodeType.Scientist: return "Scientist";
                case NodeType.Campfire: return "Campfire";
                case NodeType.RandomEvent: return "Random Event";
                default: return type.ToString();
            }
        }
        private void BeginAddConnection(MapNode node)
        {
            pendingRemoveConnectionNode = null;
            pendingAddConnectionNode = node;

            HighlightValidConnectionTargets(node);

            //MessageBox.Show(
            //    "Click another node to create a connection.",
            //    "Add Connection");
        }
        private void BeginRemoveConnection(MapNode node)
        {
            pendingAddConnectionNode = null;
            pendingRemoveConnectionNode = node;

            HighlightConnectedTargets(node);

            //MessageBox.Show(
            //    "Click a connected node to remove the connection.",
            //    "Remove Connection");
        }
        private void HighlightValidConnectionTargets(MapNode source)
        {
            foreach (var node in currentNodes)
            {
                if (node == source)
                    continue;

                //if (node.Level == source.Level)
                //    continue;
                if (source.Children.Contains(node))
                    continue;

                node.ButtonControl.BorderBrush = Brushes.Yellow;
                node.ButtonControl.BorderThickness = new Thickness(3);
            }
        }
        private void HighlightConnectedTargets(MapNode source)
        {
            foreach (var node in currentNodes)
            {
                bool connected =
                    source.Children.Contains(node) ||
                    node.Children.Contains(source);

                if (!connected)
                    continue;

                node.ButtonControl.BorderBrush = Brushes.Orange;
                node.ButtonControl.BorderThickness = new Thickness(3);
            }
        }
        private void ResetNodeBorders()
        {
            foreach (var node in currentNodes)
            {
                if (node.ButtonControl == null)
                    continue;

                node.ButtonControl.BorderThickness = new Thickness(1);

                if (node.Type == NodeType.Start)
                    node.ButtonControl.BorderBrush = Brushes.Cyan;
                else if (node.Type == NodeType.End)
                    node.ButtonControl.BorderBrush = Brushes.Red;
                else
                    node.ButtonControl.BorderBrush = Brushes.Gray;
            }
        }
        //private void FinishAddConnection(MapNode target)
        //{
        //    MapNode source = pendingAddConnectionNode;

        //    pendingAddConnectionNode = null;

        //    if (source == target)
        //    {
        //        ResetNodeBorders();
        //        return;
        //    }

        //    MapNode parent;
        //    MapNode child;

        //    if (source.Level < target.Level)
        //    {
        //        parent = source;
        //        child = target;
        //    }
        //    else
        //    {
        //        parent = target;
        //        child = source;
        //    }

        //    if (!parent.Children.Contains(child))
        //    {
        //        parent.Children.Add(child);
        //    }

        //    ResetNodeBorders();
        //    RefreshMap();
        //}
        private void FinishAddConnection(MapNode target)
        {
            MapNode source = pendingAddConnectionNode;

            pendingAddConnectionNode = null;

            if (source == null || target == null)
            {
                ResetNodeBorders();
                return;
            }

            if (source == target)
            {
                ResetNodeBorders();
                return;
            }

            // The node we started from is ALWAYS the parent.
            // The node we clicked is ALWAYS the child.
            if (!source.Children.Contains(target))
            {
                source.Children.Add(target);
                target.ParentCount++;

                if (target.Parent == null)
                {
                    target.Parent = source;
                }
            }

            ResetNodeBorders();
            RefreshMap();
        }

        private void FinishRemoveConnection(MapNode target)
        {
            MapNode source = pendingRemoveConnectionNode;

            pendingRemoveConnectionNode = null;

            if (source.Children.Contains(target))
                source.Children.Remove(target);

            if (target.Children.Contains(source))
                target.Children.Remove(source);

            ResetNodeBorders();
            RefreshMap();
        }
        private void DeleteNode(MapNode node)
        {
            if (node.Type == NodeType.Start)
            {
                MessageBox.Show("Start cannot be deleted");
                return;
            }
                

            foreach (var n in currentNodes)
            {
                n.Children.Remove(node);
            }

            currentNodes.Remove(node);

            RefreshMap();
        }
        private void RefreshMap()
        {
            MapCanvas.Children.Clear();

            foreach (var node in currentNodes)
            {
                foreach (var child in node.Children)
                {
                    DrawConnection(node.Position, child.Position);
                }
            }

            foreach (var node in currentNodes)
            {
                AddNodeButton(node);
            }
        }
        private void RenameNode(MapNode node)
        {
            Window window = new Window
            {
                Title = "Rename Node",
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = Brushes.Black,
                ResizeMode = ResizeMode.NoResize
            };

            Grid grid = new Grid
            {
                Margin = new Thickness(10)
            };

            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            TextBox textBox = new TextBox
            {
                Text = node.Name ?? "",
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 16
            };

            Grid.SetRow(textBox, 0);
            grid.Children.Add(textBox);

            StackPanel buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button okButton = new Button
            {
                Content = "OK",
                Width = 70,
                Margin = new Thickness(0, 0, 6, 0)
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Width = 70
            };

            okButton.Click += (s, e) =>
            {
                node.Name = textBox.Text.Trim();
                window.DialogResult = true;
                window.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                window.DialogResult = false;
                window.Close();
            };

            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            Grid.SetRow(buttons, 1);
            grid.Children.Add(buttons);

            window.Content = grid;

            window.ShowDialog();

            RefreshMap();
        }
        private void EditRandomEventsBtn_Click(object sender, RoutedEventArgs e)
        {
            EditRandomEvents();
        }
        void RenameNodeType(NodeType type)
        {
            string currentName = NodeLabel(new MapNode { Type = type });

            Window window = new Window();
            window.Title = "Rename " + currentName;
            window.Width = 350;
            window.Height = 150;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Owner = this;

            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(10);

            TextBox textBox = new TextBox();
            textBox.Text = currentName;
            textBox.Margin = new Thickness(0, 0, 0, 10);

            Button button = new Button();
            button.Content = "Rename";
            button.Height = 30;

            button.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    customNodeNames.Remove(type);
                }
                else
                {
                    customNodeNames[type] = textBox.Text.Trim();
                }

                window.DialogResult = true;
                window.Close();
            };

            panel.Children.Add(textBox);
            panel.Children.Add(button);

            window.Content = panel;

            window.ShowDialog();

            RefreshMap();
        }


        private void SaveMap()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Commoners Map (*.cmap)|*.cmap|JSON Files (*.json)|*.json";
            dialog.DefaultExt = ".cmap";

            if (dialog.ShowDialog() != true)
                return;

            MapSaveData saveData = new MapSaveData();

            saveData.Depth = (int)DepthSlider.Value;
            saveData.MaxBranches = (int)MaxBranchSlider.Value;

            saveData.SeedText = SeedBox.Text;
            saveData.Seed = seed;

            saveData.Zoom = ZoomSlider.Value;
            saveData.Linearity = LinearitySlider.Value;
            saveData.MaxNodesPerLevel = (int)MaxNodesPerLevelSlider.Value;

            saveData.NodeWeights = new Dictionary<int, int>();

            saveData.NodeWeights[(int)NodeType.Battle] =
                (int)WeightBattle.Value;

            saveData.NodeWeights[(int)NodeType.MiniBoss] =
                (int)WeightMiniBoss.Value;

            saveData.NodeWeights[(int)NodeType.Chest] =
                (int)WeightChest.Value;

            saveData.NodeWeights[(int)NodeType.Shop] =
                (int)WeightShop.Value;

            saveData.NodeWeights[(int)NodeType.Anvil] =
                (int)WeightAnvil.Value;

            saveData.NodeWeights[(int)NodeType.Scientist] =
                (int)WeightScientist.Value;

            saveData.NodeWeights[(int)NodeType.Campfire] =
                (int)WeightCampfire.Value;

            saveData.NodeWeights[(int)NodeType.RandomEvent] =
                (int)WeightRandomEvent.Value;

            saveData.CustomNodeNames =
                new Dictionary<int, string>();

            foreach (KeyValuePair<NodeType, string> pair in customNodeNames)
            {
                saveData.CustomNodeNames[(int)pair.Key] = pair.Value;
            }

            saveData.Nodes = new List<SavedNode>();

            foreach (MapNode node in currentNodes)
            {
                SavedNode savedNode = new SavedNode();

                savedNode.Type = (int)node.Type;
                savedNode.Name = node.Name;
                savedNode.X = node.Position.X;
                savedNode.Y = node.Position.Y;
                savedNode.Level = node.Level;
                savedNode.Note = node.Note;
                savedNode.Children = new List<int>();

                savedNode.RandomEventResolved = node.RandomEventResolved;
                savedNode.RandomEventResolvedType = (int)node.RandomEventResolvedType;
                savedNode.RandomEventScenario = node.RandomEventScenario;
                savedNode.RandomEventRevealed = node.RandomEventRevealed;

                foreach (MapNode child in node.Children)
                {
                    int childIndex = currentNodes.IndexOf(child);

                    if (childIndex >= 0)
                    {
                        savedNode.Children.Add(childIndex);
                    }
                }

                saveData.Nodes.Add(savedNode);
            }

            saveData.RandomEventSpecialWeight = randomEventSpecialWeight;
            saveData.RandomEventNodeWeight = randomEventNodeWeight;

            saveData.RandomEventBattleWeight = randomEventBattleWeight;
            saveData.RandomEventMiniBossWeight = randomEventMiniBossWeight;
            saveData.RandomEventChestWeight = randomEventChestWeight;
            saveData.RandomEventShopWeight = randomEventShopWeight;
            saveData.RandomEventAnvilWeight = randomEventAnvilWeight;
            saveData.RandomEventScientistWeight = randomEventScientistWeight;
            saveData.RandomEventCampfireWeight = randomEventCampfireWeight;

            saveData.RandomEvents = new List<RandomEventSaveData>();

            foreach (RandomEventScenario scenario in randomEventScenarios)
            {
                saveData.RandomEvents.Add(
                    new RandomEventSaveData
                    {
                        Text = scenario.Text,
                        Weight = scenario.Weight
                    });
            }


            string json = JsonConvert.SerializeObject(
                saveData,
                Formatting.Indented);

            File.WriteAllText(dialog.FileName, json);
        }


        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveMap();
        }

        private void LoadMap()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Commoners Map (*.cmap)|*.cmap|JSON Files (*.json)|*.json";

            if (dialog.ShowDialog() != true)
                return;

            string json = File.ReadAllText(dialog.FileName);

            MapSaveData saveData =
                JsonConvert.DeserializeObject<MapSaveData>(json);

            if (saveData == null)
                return;

            DepthSlider.Value = saveData.Depth;
            MaxBranchSlider.Value = saveData.MaxBranches;

            SeedBox.Text = saveData.SeedText;
            seed = saveData.Seed;

            ZoomSlider.Value = saveData.Zoom;
            LinearitySlider.Value = saveData.Linearity;
            MaxNodesPerLevelSlider.Value = saveData.MaxNodesPerLevel;

            if (saveData.NodeWeights != null)
            {
                if (saveData.NodeWeights.ContainsKey((int)NodeType.Battle))
                    WeightBattle.Value =
                        saveData.NodeWeights[(int)NodeType.Battle];

                if (saveData.NodeWeights.ContainsKey((int)NodeType.MiniBoss))
                    WeightMiniBoss.Value =
                        saveData.NodeWeights[(int)NodeType.MiniBoss];

                if (saveData.NodeWeights.ContainsKey((int)NodeType.Chest))
                    WeightChest.Value =
                        saveData.NodeWeights[(int)NodeType.Chest];

                if (saveData.NodeWeights.ContainsKey((int)NodeType.Shop))
                    WeightShop.Value =
                        saveData.NodeWeights[(int)NodeType.Shop];

                if (saveData.NodeWeights.ContainsKey((int)NodeType.Anvil))
                    WeightAnvil.Value =
                        saveData.NodeWeights[(int)NodeType.Anvil];

                if (saveData.NodeWeights.ContainsKey((int)NodeType.Scientist))
                    WeightScientist.Value =
                        saveData.NodeWeights[(int)NodeType.Scientist];

                if (saveData.NodeWeights.ContainsKey((int)NodeType.Campfire))
                    WeightCampfire.Value =
                        saveData.NodeWeights[(int)NodeType.Campfire];

                if (saveData.NodeWeights.ContainsKey((int)NodeType.RandomEvent))
                    WeightRandomEvent.Value =
                        saveData.NodeWeights[(int)NodeType.RandomEvent];
            }

            randomEventSpecialWeight =
                saveData.RandomEventSpecialWeight;

            randomEventNodeWeight =
                saveData.RandomEventNodeWeight;

            randomEventBattleWeight =
                saveData.RandomEventBattleWeight;

            randomEventMiniBossWeight =
                saveData.RandomEventMiniBossWeight;

            randomEventChestWeight =
                saveData.RandomEventChestWeight;

            randomEventShopWeight =
                saveData.RandomEventShopWeight;

            randomEventAnvilWeight =
                saveData.RandomEventAnvilWeight;

            randomEventScientistWeight =
                saveData.RandomEventScientistWeight;

            randomEventCampfireWeight =
                saveData.RandomEventCampfireWeight;

            customNodeNames.Clear();

            if (saveData.CustomNodeNames != null)
            {
                foreach (KeyValuePair<int, string> pair
                    in saveData.CustomNodeNames)
                {
                    customNodeNames[(NodeType)pair.Key] = pair.Value;
                }
            }

            randomEventScenarios.Clear();

            if (saveData.RandomEvents != null)
            {
                foreach (RandomEventSaveData savedEvent in saveData.RandomEvents)
                {
                    randomEventScenarios.Add(
                        new RandomEventScenario(
                            savedEvent.Text,
                            savedEvent.Weight));
                }
            }

            LoadSavedNodes(saveData.Nodes);
        }
        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadMap();
        }
        private void LoadSavedNodes(List<SavedNode> savedNodes)
        {
            currentNodes.Clear();

            if (savedNodes == null)
                return;

            // Create all nodes first
            for (int i = 0; i < savedNodes.Count; i++)
            {
                SavedNode saved = savedNodes[i];

                MapNode node = new MapNode();

                node.Type = (NodeType)saved.Type;
                node.Name = saved.Name;
                node.Position = new Point(saved.X, saved.Y);
                node.Level = saved.Level;
                node.Note = saved.Note;
                node.Children = new List<MapNode>();
                node.Parent = null;
                node.ParentCount = 0;

                node.RandomEventResolved = saved.RandomEventResolved;
                node.RandomEventResolvedType = (NodeType)saved.RandomEventResolvedType;
                node.RandomEventScenario = saved.RandomEventScenario;
                node.RandomEventRevealed = saved.RandomEventRevealed;

                currentNodes.Add(node);
            }

            // Recreate connections
            for (int i = 0; i < savedNodes.Count; i++)
            {
                SavedNode saved = savedNodes[i];
                MapNode node = currentNodes[i];

                if (saved.Children == null)
                    continue;

                foreach (int childIndex in saved.Children)
                {
                    if (childIndex >= 0 &&
                        childIndex < currentNodes.Count)
                    {
                        MapNode child = currentNodes[childIndex];

                        node.Children.Add(child);
                        child.ParentCount++;

                        if (child.Parent == null)
                        {
                            child.Parent = node;
                        }
                    }
                }
            }

            RefreshMap();
        }

        private void EditNodeNote(MapNode node)
        {
            Window window = new Window();

            window.Title = "Note - " + NodeLabel(node);
            window.Width = 450;
            window.Height = 300;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Owner = this;

            Grid grid = new Grid();
            grid.Margin = new Thickness(10);

            RowDefinition row1 = new RowDefinition();
            row1.Height = new GridLength(1, GridUnitType.Star);

            RowDefinition row2 = new RowDefinition();
            row2.Height = GridLength.Auto;

            grid.RowDefinitions.Add(row1);
            grid.RowDefinitions.Add(row2);

            TextBox textBox = new TextBox();

            textBox.Text = node.Note;
            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            Grid.SetRow(textBox, 0);

            Button saveButton = new Button();
            saveButton.Content = "Save Note";
            saveButton.Height = 30;
            saveButton.Margin = new Thickness(0, 10, 0, 0);

            saveButton.Click += delegate
            {
                node.Note = textBox.Text;

                window.DialogResult = true;
                window.Close();
            };

            Grid.SetRow(saveButton, 1);

            grid.Children.Add(textBox);
            grid.Children.Add(saveButton);

            window.Content = grid;

            window.ShowDialog();
        }

        private void ShowNodeNote(MapNode node)
        {
            string text =
                "Node: " + NodeLabel(node) +
                "\n(Level " + node.Level + ")";

            if (!string.IsNullOrWhiteSpace(node.Note))
            {
                text += "\n\n" + node.Note;
            }

            MessageBox.Show(
                text,
                NodeLabel(node),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void MapCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If the right-click came from a node button,
            // let that node's context menu handle it.
            DependencyObject source = e.OriginalSource as DependencyObject;

            while (source != null)
            {
                if (source is Button)
                {
                    return;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            Point position = e.GetPosition(MapCanvas);

            ContextMenu menu = new ContextMenu();

            MenuItem addNodeItem = new MenuItem();
            addNodeItem.Header = "Add Node";

            MenuItem battleItem = new MenuItem();
            battleItem.Header = "Battle";
            battleItem.Click += delegate
            {
                AddManualNode(NodeType.Battle, position);
            };

            MenuItem miniBossItem = new MenuItem();
            miniBossItem.Header = "Mini Boss";
            miniBossItem.Click += delegate
            {
                AddManualNode(NodeType.MiniBoss, position);
            };

            MenuItem chestItem = new MenuItem();
            chestItem.Header = "Chest";
            chestItem.Click += delegate
            {
                AddManualNode(NodeType.Chest, position);
            };

            MenuItem shopItem = new MenuItem();
            shopItem.Header = "Shop";
            shopItem.Click += delegate
            {
                AddManualNode(NodeType.Shop, position);
            };

            MenuItem anvilItem = new MenuItem();
            anvilItem.Header = "Anvil";
            anvilItem.Click += delegate
            {
                AddManualNode(NodeType.Anvil, position);
            };

            MenuItem scientistItem = new MenuItem();
            scientistItem.Header = "Scientist";
            scientistItem.Click += delegate
            {
                AddManualNode(NodeType.Scientist, position);
            };

            MenuItem campfireItem = new MenuItem();
            campfireItem.Header = "Campfire";
            campfireItem.Click += delegate
            {
                AddManualNode(NodeType.Campfire, position);
            };

            MenuItem randomEventItem = new MenuItem();
            randomEventItem.Header = "Random Event";
            randomEventItem.Click += delegate
            {
                AddManualNode(NodeType.RandomEvent, position);
            };
            MenuItem endItem = new MenuItem();
            endItem.Header = "End";
            endItem.Click += delegate 
            {
                AddManualNode(NodeType.End, position);
            };

            addNodeItem.Items.Add(battleItem);
            addNodeItem.Items.Add(miniBossItem);
            addNodeItem.Items.Add(chestItem);
            addNodeItem.Items.Add(shopItem);
            addNodeItem.Items.Add(anvilItem);
            addNodeItem.Items.Add(scientistItem);
            addNodeItem.Items.Add(campfireItem);
            addNodeItem.Items.Add(randomEventItem);
            addNodeItem.Items.Add(endItem);

            menu.Items.Add(addNodeItem);

            menu.IsOpen = true;

            e.Handled = true;
        }
        private void AddManualNode(NodeType type, Point position)
        {

            MapNode node = new MapNode();

            node.Type = type;
            node.Position = position;
            node.Level = 0;
            node.Children = new List<MapNode>();
            node.Parent = null;
            node.ParentCount = 0;
            node.Note = "";

            currentNodes.Add(node);

            RefreshMap();
        }

    }
    public class MapSaveData
    {
        public int Depth { get; set; }
        public int MaxBranches { get; set; }
        public int Stage { get; set; }

        public string SeedText { get; set; }
        public int Seed { get; set; }

        public double Zoom { get; set; }
        public double Linearity { get; set; }
        public int MaxNodesPerLevel { get; set; }

        public Dictionary<int, int> NodeWeights { get; set; }

        public Dictionary<int, string> CustomNodeNames { get; set; }

        public List<string> RandomEventScenarios { get; set; }

        public List<SavedNode> Nodes { get; set; }

        public double RandomEventSpecialWeight { get; set; }
        public double RandomEventNodeWeight { get; set; }

        public double RandomEventBattleWeight { get; set; }
        public double RandomEventMiniBossWeight { get; set; }
        public double RandomEventChestWeight { get; set; }
        public double RandomEventShopWeight { get; set; }
        public double RandomEventAnvilWeight { get; set; }
        public double RandomEventScientistWeight { get; set; }
        public double RandomEventCampfireWeight { get; set; }

        public List<RandomEventSaveData> RandomEvents { get; set; }
    }

    public class RandomEventSaveData
    {
        public string Text { get; set; }
        public double Weight { get; set; }
    }

    public class SavedNode
    {
        public int Type { get; set; }
        public string Name { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public int Level { get; set; }

        public string Note { get; set; }

        public List<int> Children { get; set; }

        public bool RandomEventResolved { get; set; }
        public int RandomEventResolvedType { get; set; }
        public string RandomEventScenario { get; set; }
        public bool RandomEventRevealed { get; set; }
    }



}
