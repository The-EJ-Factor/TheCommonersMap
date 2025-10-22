using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace TheCommonersMap
{
    public partial class MainWindow : Window
    {
        enum NodeType { Start, Battle, MiniBoss, Chest, Shop, Gym, Campfire, RandomEvent, FinalBoss }

        class MapNode
        {
            public NodeType Type;
            public Point Position;
            public List<MapNode> Children = new List<MapNode>();
            public MapNode Parent;
            public int ParentCount;
            public Button ButtonControl;
            public int Level;
        }


        readonly Dictionary<NodeType, Brush> TypeColors = new Dictionary<NodeType, Brush>()
        {
            { NodeType.Start, Brushes.Cyan },
            { NodeType.Battle, Brushes.LightBlue },
            { NodeType.MiniBoss, Brushes.Orange },
            { NodeType.Chest, Brushes.Gold },
            { NodeType.Shop, Brushes.LightGreen },
            { NodeType.Gym, Brushes.Violet },
            { NodeType.Campfire, Brushes.LightSalmon },
            { NodeType.RandomEvent, Brushes.White },
            { NodeType.FinalBoss, Brushes.Red }
        };

        Random rng = new Random();
        int seed = Environment.TickCount;

        // editable scenario list for Random Event (extend in code)
        List<string> randomEventScenarios = new List<string>()
        {
            "You find a lost traveler who offers a useful item.",
            "A trap! Avoid or take damage.",
            "A fortune teller offers a prophecy (free info).",
            "An NPC asks for help — choose reward."
        };

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
        (WeightGym, WeightGymLabel),
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
            // easier: more chests/shops, fewer battles
            WeightBattle.Value = 5;
            WeightMiniBoss.Value = 0;
            WeightChest.Value = 5;
            WeightShop.Value = 3;
            WeightGym.Value = 1;
            WeightCampfire.Value = 3;
            WeightRandomEvent.Value = 1;
        }

        private void ApplyNormal_Click(object sender, RoutedEventArgs e)
        {
            WeightBattle.Value = 6;
            WeightMiniBoss.Value = 1;
            WeightChest.Value = 3;
            WeightShop.Value = 2;
            WeightGym.Value = 1;
            WeightCampfire.Value = 2;
            WeightRandomEvent.Value = 1;
        }

        private void ApplyHard_Click(object sender, RoutedEventArgs e)
        {
            WeightBattle.Value = 8;
            WeightMiniBoss.Value = 2;
            WeightChest.Value = 1;
            WeightShop.Value = 0;
            WeightGym.Value = 0;
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

            // assemble weight table (ignore Start & FinalBoss)
            var weights = new Dictionary<NodeType, int>
            {
                { NodeType.Battle, (int)WeightBattle.Value },
                { NodeType.MiniBoss, (int)WeightMiniBoss.Value },
                { NodeType.Chest, (int)WeightChest.Value },
                { NodeType.Shop, (int)WeightShop.Value },
                { NodeType.Gym, (int)WeightGym.Value },
                { NodeType.Campfire, (int)WeightCampfire.Value },
                { NodeType.RandomEvent, (int)WeightRandomEvent.Value }
            };

            // Build level-by-level nodes (levels 0..depth-1), final boss at level depth
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

            // --- FINAL BOSS ---
            var finalBoss = new MapNode { Level = depth, Type = NodeType.FinalBoss };
            levels.Add(new List<MapNode> { finalBoss });

            // Track how many parents the boss has
            int bossParentCount = 0;

            foreach (var parent in levels[depth - 1])
            {
                if (parent.Children.Count < maxBranches && bossParentCount < maxBranches)
                {
                    parent.Children.Add(finalBoss);
                    if (finalBoss.Parent == null)
                        finalBoss.Parent = parent;
                    bossParentCount++;
                }
            }

            // Ensure the boss has at least 1 parent
            if (finalBoss.Parent == null && levels[depth - 1].Count > 0)
            {
                finalBoss.Parent = levels[depth - 1][0];
                levels[depth - 1][0].Children.Add(finalBoss);
            }



            // Add final boss as its own single level (we will use levelsCount = depth+1 for layout)
            var allLevels = new List<List<MapNode>>(levels);
            allLevels.Add(new List<MapNode> { finalBoss });

            // layout: compute width per level and set canvas size
            double horizontalSpacing = 160; // base spacing
            double verticalSpacing = 140;
            // compute widest level
            int widest = allLevels.Max(l => l.Count);
            double canvasWidth = Math.Max(800, widest * horizontalSpacing + 200);
            double canvasHeight = (allLevels.Count + 1) * verticalSpacing + 200;

            MapCanvas.Width = canvasWidth;
            MapCanvas.Height = canvasHeight;
            MapCanvas.Background = Brushes.Black;

            // position nodes in each level centered horizontally
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

            // draw lines first (parents -> children)
            // gather unique nodes
            var nodes = allLevels.SelectMany(l => l).ToList();

            // draw connections (white)
            foreach (var node in nodes)
            {
                foreach (var child in node.Children)
                {
                    DrawConnection(node.Position, child.Position);
                }
            }

            // add buttons for nodes (on top)
            foreach (var node in nodes)
            {
                AddNodeButton(node);
            }
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
            node.ButtonControl = btn;

            // special visual for Start and FinalBoss
            if (node.Type == NodeType.Start)
            {
                btn.BorderBrush = Brushes.Cyan;
                btn.FontWeight = FontWeights.Bold;
                btn.Width = 140;
            }
            else if (node.Type == NodeType.FinalBoss)
            {
                btn.BorderBrush = Brushes.Red;
                btn.Width = 160;
                btn.FontWeight = FontWeights.Bold;
            }
            else if (node.Type == NodeType.RandomEvent)
            {
                // show just ? for the user-specified request but keep full type as ToolTip
                btn.Content = "?";
                btn.ToolTip = "Random Event (click to resolve)";
            }

            MapCanvas.Children.Add(btn);
        }

        string NodeLabel(MapNode node)
        {
            switch (node.Type)
            {
                case NodeType.Start: return "Start";
                case NodeType.Battle: return "Battle";
                case NodeType.MiniBoss: return "Mini Boss/Special Encounter";
                case NodeType.Chest: return "Chest";
                case NodeType.Shop: return "Shop";
                case NodeType.Gym: return "Gym";
                case NodeType.Campfire: return "Campfire";
                case NodeType.RandomEvent: return "?";
                case NodeType.FinalBoss: return "Boss";
                default: return node.Type.ToString();
            }
        }

        private void NodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button b)) return;
            var node = b.Tag as MapNode;
            if (node == null) return;

            switch (node.Type)
            {
                case NodeType.RandomEvent:
                    ResolveRandomEvent(node, b);
                    break;
                case NodeType.Battle:
                case NodeType.MiniBoss:
                case NodeType.Chest:
                case NodeType.Shop:
                case NodeType.Gym:
                case NodeType.Campfire:
                    MessageBox.Show($"Node: {NodeLabel(node)}\n(Level {node.Level})", "Node Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case NodeType.Start:
                    MessageBox.Show("Start node. Prepare for the journey!", "Start", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case NodeType.FinalBoss:
                    MessageBox.Show("Final Boss!\nThis ends the run.", "Final Boss", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }

        private void ResolveRandomEvent(MapNode node, Button button)
        {
            // 50% chance to convert into another node type, otherwise show scenario
            int choice = rng.Next(100);
            if (choice < 50)
            {
                // convert into one of the other types (weighted by same weights)
                var weights = new Dictionary<NodeType, int>
                {
                    { NodeType.Battle, (int)WeightBattle.Value },
                    { NodeType.MiniBoss, (int)WeightMiniBoss.Value },
                    { NodeType.Chest, (int)WeightChest.Value },
                    { NodeType.Shop, (int)WeightShop.Value },
                    { NodeType.Gym, (int)WeightGym.Value },
                    { NodeType.Campfire, (int)WeightCampfire.Value },
                    { NodeType.RandomEvent, (int)WeightRandomEvent.Value } // could convert to another random event
                };
                var newType = PickNodeTypeByWeight(weights);
                node.Type = newType;

                // update visual
                button.Foreground = TypeColors[newType];
                button.Content = NodeLabel(node);
                button.ToolTip = null;
                MessageBox.Show($"Random Event resolved into: {NodeLabel(node)}", "Random Event", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Show a random scenario from list
                int i = rng.Next(randomEventScenarios.Count);
                MessageBox.Show(randomEventScenarios[i], "Random Event", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
