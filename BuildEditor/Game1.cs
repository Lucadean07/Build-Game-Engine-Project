using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using Myra;
using Myra.Graphics2D.UI;

namespace BuildEditor
{
    public class BuildLevelEditor : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Camera2D _camera;
        private Texture2D _pixelTexture;
        private Dictionary<string, Texture2D> _spriteTextures;
        private Dictionary<string, Texture2D> _geometryTextures;

        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;

        private List<Sector> _sectors;
        private int _nextSectorId = 0;
        private int _nextSpriteId = 0;
        private EditMode _currentEditMode = EditMode.VertexPlacement;
        private Vector2? _hoveredVertex;
        private Vector2 _mouseWorldPosition;

        // 3D cursor and interaction system
        private Vector3 _cursor3DPosition;
        private Vector3 _cursor3DNormal;
        private string _cursor3DSnapType = "none"; // "floor", "wall", "ceiling", "none"
        private Sector _cursor3DSector;
        private Wall _cursor3DWall;

        // 3D sprite interaction
        private bool _isDragging3DSprite = false;
        private Sprite _hoveredSprite3D = null;
        private Vector3 _dragOffset3D;
        private bool _spriteMouseTrackMode = false;
        private Vector2 _lastSpriteTrackPosition = Vector2.Zero;
        private float _lastSpriteTrackHeight = 64f;
        private float _lastSpriteTrackAngle = 0f;
        private SpriteAlignment _lastSpriteTrackAlignment = SpriteAlignment.Floor;

        // 3D gizmo system
        private Sprite _selectedSprite3D = null;
        private string _hoveredGizmo = "none"; // "x", "y", "z", "none"
        private bool _isDraggingGizmo = false;
        private Vector3 _gizmoStartPosition;
        private Vector2 _dragStartMouse;



        // Selection mode variables
        private List<Vector2> _selectedVertices = new List<Vector2>();
        private Vector2? _dragStart;
        private bool _isDragging;
        private Sector _selectedSector;
        private Sprite _selectedSprite;
        private Wall _selectedWall;

        private Wall _hoveredWall;
        

        // Sprite editor UI components
        private Window _spriteEditorWindow;
        private bool _spriteEditorVisible = false;

        // Myra UI
        private Desktop _desktop;
        private Panel _propertiesPanel;
        private Panel _toolsPanel;
        private Panel _statusPanel;
        private Label _cameraLabel;
        private Label _zoomLabel;
        private Label _mouseLabel;
        private Label _sectorsLabel;
        private Label _verticesLabel;
        private Label _wallsLabel;
        private Label _statusLabel;
        private RadioButton _vertexModeButton;
        private RadioButton _selectionModeButton;
        private RadioButton _deleteModeButton;
        private RadioButton _spritePlaceModeButton;
        private RadioButton _slopeModeButton;
        private Button _floorSlopeButton;
        private Button _ceilingSlopeButton;

        // Nested sector creation modes
        private Button _independentSectorButton;
        private Button _nestedSectorButton;
        private bool _createNestedSector = false;

        // Player position system
        private Vector2 _playerPosition = new Vector2(0, 0);
        private bool _hasPlayerPosition = true; // Start with player spawned
        private bool _playerSelected = false;
        private bool _draggingPlayer = false;
        private Vector2 _playerDragStart;
        private bool _draggingSprite = false;
        private Vector2 _spriteDragStart;
        
        // Collision system
        private bool _collisionMode = false;
        private const float PLAYER_RADIUS = 8f; // Build engine player radius (smaller than default 16)


        // Sector properties UI
        private Window _sectorPropertiesWindow;
        private Label _sectorIdLabel;
        private TextBox _floorHeightTextBox;
        private TextBox _ceilingHeightTextBox;
        private TextBox _sectorLoTagTextBox;
        private TextBox _sectorHiTagTextBox;
        private Button _floorTextureButton;
        private Button _ceilingTextureButton;
        private Button _wallTextureButton;

        // Lift controls
        private CheckButton _isLiftCheckBox;
        private TextBox _liftLowHeightTextBox;
        private TextBox _liftHighHeightTextBox;
        private TextBox _liftSpeedTextBox;
        private bool _propertiesWindowVisible = false;

        private Button _frameTextureButton;

        // Tag system constants for behaviors and linking
        public static class TagConstants
        {
            // Common LoTag (behavior) values for sectors
            public const int SECTOR_NORMAL = 0;
            public const int SECTOR_DOOR = 1;
            public const int SECTOR_LIFT = 2;
            public const int SECTOR_WATER = 3;
            public const int SECTOR_DAMAGE = 4;
            public const int SECTOR_TELEPORTER = 5;

            // Common LoTag (behavior) values for sprites
            public const int SPRITE_DECORATION = 0;
            public const int SPRITE_ENEMY = 1;
            public const int SPRITE_PICKUP = 2;
            public const int SPRITE_TRIGGER = 3;
            public const int SPRITE_LIGHT = 4;
            public const int SPRITE_SOUND = 5;
            public const int SPRITE_SWITCH = 6;
            public const int SPRITE_TELEPORTER_EXIT = 7;
        }

        // Texture system
        private readonly Dictionary<string, Color> _textureColors = new Dictionary<string, Color>
        {
            { "White", Color.White },
            { "Gray", Color.Gray },
            { "LightGray", Color.LightGray },
            { "Brown", Color.SaddleBrown },
            { "DarkGreen", Color.DarkGreen }
        };

        // Actual texture loading system
        private Dictionary<string, Texture2D> _loadedTextures = new Dictionary<string, Texture2D>();
        private Texture2D _defaultTexture;

        // Texture dropdown
        private Window _textureDropdownWindow;
        private string _currentTextureType;

        // Texture Editor Window
        private Window _textureEditorWindow;
        private bool _textureEditorVisible = false;
        private Button _floorTextureButtonTex, _ceilingTextureButtonTex, _wallTextureButtonTex;
        private CheckButton _floorUVFlipXCheck, _floorUVFlipYCheck;
        private CheckButton _ceilingUVFlipXCheck, _ceilingUVFlipYCheck;
        private CheckButton _wallUVFlipXCheck, _wallUVFlipYCheck;
        private TextBox _floorUVOffsetXBox, _floorUVOffsetYBox;
        private TextBox _ceilingUVOffsetXBox, _ceilingUVOffsetYBox;
        private TextBox _wallUVOffsetXBox, _wallUVOffsetYBox;
        private TextBox _floorUVRotationBox, _ceilingUVRotationBox, _wallUVRotationBox;
        private TextBox _floorUVScaleXBox, _floorUVScaleYBox;
        private TextBox _ceilingUVScaleXBox, _ceilingUVScaleYBox;
        private TextBox _wallUVScaleXBox, _wallUVScaleYBox;
        private TextBox _floorShadingBox, _ceilingShadingBox, _wallShadingBox;

        // Sprite editor UI fields
        private TextBox _spritePositionXBox, _spritePositionYBox;
        private TextBox _spriteAngleBox;
        private TextBox _spriteScaleXBox, _spriteScaleYBox;
        private TextBox _spritePaletteBox;
        private TextBox _spriteLoTagBox;
        private TextBox _spriteHiTagBox;
        private ComboView _spriteAlignmentBox;
        private ComboView _spriteTagBox;
        private bool _isUpdatingSpriteUI = false;

        // 3D rendering variables
        private bool _is3DMode = false;
        private bool _wireframeMode = false;
        private bool _autoAlignMode = false;
        private Vector3 _lastAutoAlignPosition = Vector3.Zero;

        // Slope editing system
        private Vector2? _selectedVertex;
        private bool _isDraggingVertex = false;
        private float _vertexDragStartHeight = 0f;
        private bool _isEditingFloorSlope = true; // true = floor, false = ceiling
        private Vector3 _camera3DPosition = new Vector3(0, 30, -100);
        private Vector3 _camera3DTarget = Vector3.Zero;
        private float _camera3DYaw = 0f;
        private float _camera3DPitch = 0f;
        private Button _wireframeButton;
        private BasicEffect _basicEffect;
        private Button _clearButton;
        private Button _resetButton;

        public BuildLevelEditor()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.ApplyChanges();

            _sectors = new List<Sector>();
            _camera = new Camera2D();
            _spriteTextures = new Dictionary<string, Texture2D>();
            _geometryTextures = new Dictionary<string, Texture2D>();
        }

        protected override void Initialize()
        {
            MyraEnvironment.Game = this;
            SetupUi();

            base.Initialize();
        }

        private void SetupUi()
        {
            _desktop = new Desktop();

            // Properties Panel
            _propertiesPanel = new Panel
            {
                Left = 10,
                Top = 10,
                Width = 230,
                Height = 300
            };

            var propertiesGrid = new Grid
            {
                RowSpacing = 5,
                ColumnSpacing = 5
            };
            propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

            var titleLabel = new Label { Text = "Properties", TextColor = Color.White };
            propertiesGrid.Widgets.Add(titleLabel);
            Grid.SetRow(titleLabel, 0);

            _cameraLabel = new Label { Text = "Camera: 0, 0", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(_cameraLabel);
            Grid.SetRow(_cameraLabel, 1);

            _zoomLabel = new Label { Text = "Zoom: 1.00", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(_zoomLabel);
            Grid.SetRow(_zoomLabel, 2);

            _mouseLabel = new Label { Text = "Mouse: 0, 0", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(_mouseLabel);
            Grid.SetRow(_mouseLabel, 3);

            var separatorLabel = new Label { Text = "Sectors", TextColor = Color.White };
            propertiesGrid.Widgets.Add(separatorLabel);
            Grid.SetRow(separatorLabel, 4);

            _sectorsLabel = new Label { Text = "Count: 0", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(_sectorsLabel);
            Grid.SetRow(_sectorsLabel, 5);

            _verticesLabel = new Label { Text = "Vertices: 0", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(_verticesLabel);
            Grid.SetRow(_verticesLabel, 6);

            _wallsLabel = new Label { Text = "Walls: 0", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(_wallsLabel);
            Grid.SetRow(_wallsLabel, 7);

            _propertiesPanel.Widgets.Add(propertiesGrid);
            _desktop.Widgets.Add(_propertiesPanel);

            // Tools Panel
            _toolsPanel = new Panel
            {
                Left = 10,
                Top = 320,
                Width = 230,
                Height = 350
            };

            var toolsGrid = new Grid
            {
                RowSpacing = 5,
                ColumnSpacing = 5
            };
            for (int i = 0; i < 20; i++)
            {
                toolsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            }

            toolsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

            var toolsTitle = new Label { Text = "Tools", TextColor = Color.White };
            toolsGrid.Widgets.Add(toolsTitle);
            Grid.SetRow(toolsTitle, 0);

            _vertexModeButton = new RadioButton
                { Content = new Label { Text = "Vertex Placement", TextColor = Color.LightGray }, IsPressed = true };
            _vertexModeButton.Click += (s, e) => _currentEditMode = EditMode.VertexPlacement;
            toolsGrid.Widgets.Add(_vertexModeButton);
            Grid.SetRow(_vertexModeButton, 1);

            _selectionModeButton = new RadioButton
                { Content = new Label { Text = "Selection", TextColor = Color.LightGray } };
            _selectionModeButton.Click += (s, e) => _currentEditMode = EditMode.Selection;
            toolsGrid.Widgets.Add(_selectionModeButton);
            Grid.SetRow(_selectionModeButton, 2);

            _deleteModeButton = new RadioButton
                { Content = new Label { Text = "Delete", TextColor = Color.LightGray } };
            _deleteModeButton.Click += (s, e) => _currentEditMode = EditMode.Delete;
            toolsGrid.Widgets.Add(_deleteModeButton);
            Grid.SetRow(_deleteModeButton, 3);


            _spritePlaceModeButton = new RadioButton
                { Content = new Label { Text = "Sprite", TextColor = Color.LightGray } };
            _spritePlaceModeButton.Click += (s, e) => { _currentEditMode = EditMode.SpritePlace; };
            toolsGrid.Widgets.Add(_spritePlaceModeButton);
            Grid.SetRow(_spritePlaceModeButton, 4);

            _slopeModeButton = new RadioButton { Content = new Label { Text = "Slope", TextColor = Color.LightGray } };
            _slopeModeButton.Click += (s, e) => { _currentEditMode = EditMode.SlopeEdit; };
            toolsGrid.Widgets.Add(_slopeModeButton);
            Grid.SetRow(_slopeModeButton, 5);

            // Slope toggle buttons - only visible in slope mode
            _floorSlopeButton = new Button { Content = new Label { Text = "Edit Floor Slope" }, Visible = false };
            _floorSlopeButton.Click += (s, e) =>
            {
                _isEditingFloorSlope = true;
                UpdateSlopeButtonStates();
            };
            toolsGrid.Widgets.Add(_floorSlopeButton);
            Grid.SetRow(_floorSlopeButton, 8);

            _ceilingSlopeButton = new Button { Content = new Label { Text = "Edit Ceiling Slope" }, Visible = false };
            _ceilingSlopeButton.Click += (s, e) =>
            {
                _isEditingFloorSlope = false;
                UpdateSlopeButtonStates();
            };
            toolsGrid.Widgets.Add(_ceilingSlopeButton);
            Grid.SetRow(_ceilingSlopeButton, 9);

            _clearButton = new Button { Content = new Label { Text = "Clear All Sectors" } };
            _clearButton.Click += (s, e) =>
            {
                _sectors.Clear();
                _nextSectorId = 0;
                _nextSpriteId = 0;
                _selectedSprite = null;
            };
            toolsGrid.Widgets.Add(_clearButton);
            Grid.SetRow(_clearButton, 11);

            _resetButton = new Button { Content = new Label { Text = "Reset Camera" } };
            _resetButton.Click += (s, e) =>
            {
                _camera.Position = Vector2.Zero;
                _camera.Zoom = 1.0f;
            };
            toolsGrid.Widgets.Add(_resetButton);
            Grid.SetRow(_resetButton, 12);

            // 3D toggle moved to Tab key, wireframe button only (F key also works)
            _wireframeButton = new Button { Content = new Label { Text = "Wireframe (F)" } };
            _wireframeButton.Click += (s, e) =>
            {
                if (_is3DMode) _wireframeMode = !_wireframeMode;
            };
            toolsGrid.Widgets.Add(_wireframeButton);
            Grid.SetRow(_wireframeButton, 13);

            // Sector toggle buttons - only visible in vertex placement mode
            _independentSectorButton = new Button { Content = new Label { Text = "Independent" }, Visible = false };
            _independentSectorButton.Click += (s, e) =>
            {
                Console.WriteLine("*** INDEPENDENT BUTTON CLICKED ***");
                _createNestedSector = false;
                UpdateSectorModeControls();
                Console.WriteLine($"Mode is now: {(_createNestedSector ? "NESTED" : "INDEPENDENT")}");
            };
            toolsGrid.Widgets.Add(_independentSectorButton);
            Grid.SetRow(_independentSectorButton, 15);

            _nestedSectorButton = new Button { Content = new Label { Text = "Nested" }, Visible = false };
            _nestedSectorButton.Click += (s, e) =>
            {
                Console.WriteLine("*** NESTED BUTTON CLICKED ***");
                _createNestedSector = true;
                UpdateSectorModeControls();
                Console.WriteLine($"Mode is now: {(_createNestedSector ? "NESTED" : "INDEPENDENT")}");
            };
            toolsGrid.Widgets.Add(_nestedSectorButton);
            Grid.SetRow(_nestedSectorButton, 16);

            // Player spawns automatically at (0,0) and can be selected/dragged



            _toolsPanel.Widgets.Add(toolsGrid);
            _desktop.Widgets.Add(_toolsPanel);

            SetupSectorPropertiesWindow();
            SetupTextureEditorWindow();
            SetupSpriteEditorWindow();

            // Status Panel
            _statusPanel = new Panel
            {
                Left = 250,
                Top = GraphicsDevice.Viewport.Height - 30,
                Width = GraphicsDevice.Viewport.Width - 250,
                Height = 30
            };

            _statusLabel = new Label
            {
                Text = "Ready",
                TextColor = Color.Yellow,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _statusPanel.Widgets.Add(_statusLabel);
            _desktop.Widgets.Add(_statusPanel);
        }

        private void UpdateSlopeButtonStates()
        {
            if (_floorSlopeButton != null)
            {
                ((Label)_floorSlopeButton.Content).TextColor = _isEditingFloorSlope ? Color.Yellow : Color.White;
            }

            if (_ceilingSlopeButton != null)
            {
                ((Label)_ceilingSlopeButton.Content).TextColor = _isEditingFloorSlope ? Color.White : Color.Yellow;
            }
        }

        private void UpdateSectorModeControls()
        {
            if (_independentSectorButton != null)
            {
                ((Label)_independentSectorButton.Content).TextColor = _createNestedSector ? Color.White : Color.Yellow;
            }

            if (_nestedSectorButton != null)
            {
                ((Label)_nestedSectorButton.Content).TextColor = _createNestedSector ? Color.Yellow : Color.White;
            }
        }

        private void SetupSectorPropertiesWindow()
        {
            _sectorPropertiesWindow = new Window
            {
                Title = "Sector Properties",
                Left = 300,
                Top = 100,
                Width = 320,
                Height = 280,
                Visible = false // Initially hidden
            };

            var propertiesGrid = new Grid
            {
                RowSpacing = 5,
                ColumnSpacing = 5
            };

            for (int i = 0; i < 12; i++)
            {
                propertiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            }

            propertiesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            propertiesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            propertiesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            // Sector ID label
            _sectorIdLabel = new Label { Text = "Sector Properties", TextColor = Color.White };
            propertiesGrid.Widgets.Add(_sectorIdLabel);
            Grid.SetRow(_sectorIdLabel, 0);
            Grid.SetColumn(_sectorIdLabel, 0);
            Grid.SetColumnSpan(_sectorIdLabel, 2);

            // Floor Height
            var floorLabel = new Label { Text = "Floor:", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(floorLabel);
            Grid.SetRow(floorLabel, 1);
            Grid.SetColumn(floorLabel, 0);

            _floorHeightTextBox = new TextBox { Width = 60 };
            propertiesGrid.Widgets.Add(_floorHeightTextBox);
            Grid.SetRow(_floorHeightTextBox, 1);
            Grid.SetColumn(_floorHeightTextBox, 1);

            var floorMinusBtn = new Button { Content = new Label { Text = "-" }, Width = 25 };
            floorMinusBtn.Click += (s, e) => AdjustFloorHeight(-4f);
            propertiesGrid.Widgets.Add(floorMinusBtn);
            Grid.SetRow(floorMinusBtn, 1);
            Grid.SetColumn(floorMinusBtn, 2);

            var floorPlusBtn = new Button { Content = new Label { Text = "+" }, Width = 25 };
            floorPlusBtn.Click += (s, e) => AdjustFloorHeight(4f);
            propertiesGrid.Widgets.Add(floorPlusBtn);
            Grid.SetRow(floorPlusBtn, 1);
            Grid.SetColumn(floorPlusBtn, 3);

            // Ceiling Height
            var ceilingLabel = new Label { Text = "Ceiling:", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(ceilingLabel);
            Grid.SetRow(ceilingLabel, 2);
            Grid.SetColumn(ceilingLabel, 0);

            _ceilingHeightTextBox = new TextBox { Width = 60 };
            propertiesGrid.Widgets.Add(_ceilingHeightTextBox);
            Grid.SetRow(_ceilingHeightTextBox, 2);
            Grid.SetColumn(_ceilingHeightTextBox, 1);

            var ceilingMinusBtn = new Button { Content = new Label { Text = "-" }, Width = 25 };
            ceilingMinusBtn.Click += (s, e) => AdjustCeilingHeight(-4f);
            propertiesGrid.Widgets.Add(ceilingMinusBtn);
            Grid.SetRow(ceilingMinusBtn, 2);
            Grid.SetColumn(ceilingMinusBtn, 2);

            var ceilingPlusBtn = new Button { Content = new Label { Text = "+" }, Width = 25 };
            ceilingPlusBtn.Click += (s, e) => AdjustCeilingHeight(4f);
            propertiesGrid.Widgets.Add(ceilingPlusBtn);
            Grid.SetRow(ceilingPlusBtn, 2);
            Grid.SetColumn(ceilingPlusBtn, 3);

            // LoTag (Behavior Tag)
            var loTagLabel = new Label { Text = "LoTag:", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(loTagLabel);
            Grid.SetRow(loTagLabel, 3);
            Grid.SetColumn(loTagLabel, 0);

            _sectorLoTagTextBox = new TextBox { Width = 60 };
            propertiesGrid.Widgets.Add(_sectorLoTagTextBox);
            Grid.SetRow(_sectorLoTagTextBox, 3);
            Grid.SetColumn(_sectorLoTagTextBox, 1);

            // HiTag (Link Tag)
            var hiTagLabel = new Label { Text = "HiTag:", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(hiTagLabel);
            Grid.SetRow(hiTagLabel, 4);
            Grid.SetColumn(hiTagLabel, 0);

            _sectorHiTagTextBox = new TextBox { Width = 60 };
            propertiesGrid.Widgets.Add(_sectorHiTagTextBox);
            Grid.SetRow(_sectorHiTagTextBox, 4);
            Grid.SetColumn(_sectorHiTagTextBox, 1);

            // Lift Checkbox
            var liftLabel = new Label { Text = "Is Lift:", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(liftLabel);
            Grid.SetRow(liftLabel, 5);
            Grid.SetColumn(liftLabel, 0);

            _isLiftCheckBox = new CheckButton();
            propertiesGrid.Widgets.Add(_isLiftCheckBox);
            Grid.SetRow(_isLiftCheckBox, 5);
            Grid.SetColumn(_isLiftCheckBox, 1);

            // Lift Low Height
            var liftLowLabel = new Label { Text = "Low Height:", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(liftLowLabel);
            Grid.SetRow(liftLowLabel, 6);
            Grid.SetColumn(liftLowLabel, 0);

            _liftLowHeightTextBox = new TextBox { Width = 60 };
            propertiesGrid.Widgets.Add(_liftLowHeightTextBox);
            Grid.SetRow(_liftLowHeightTextBox, 6);
            Grid.SetColumn(_liftLowHeightTextBox, 1);

            // Lift High Height
            var liftHighLabel = new Label { Text = "High Height:", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(liftHighLabel);
            Grid.SetRow(liftHighLabel, 7);
            Grid.SetColumn(liftHighLabel, 0);

            _liftHighHeightTextBox = new TextBox { Width = 60 };
            propertiesGrid.Widgets.Add(_liftHighHeightTextBox);
            Grid.SetRow(_liftHighHeightTextBox, 7);
            Grid.SetColumn(_liftHighHeightTextBox, 1);

            // Lift Speed
            var liftSpeedLabel = new Label { Text = "Lift Speed:", TextColor = Color.LightGray };
            propertiesGrid.Widgets.Add(liftSpeedLabel);
            Grid.SetRow(liftSpeedLabel, 8);
            Grid.SetColumn(liftSpeedLabel, 0);

            _liftSpeedTextBox = new TextBox { Width = 60 };
            propertiesGrid.Widgets.Add(_liftSpeedTextBox);
            Grid.SetRow(_liftSpeedTextBox, 8);
            Grid.SetColumn(_liftSpeedTextBox, 1);

            // Texture Editor Button
            var textureEditorButton = new Button { Content = new Label { Text = "Open Texture Editor" }, Width = 150 };
            textureEditorButton.Click += (s, e) =>
            {
                _textureEditorVisible = !_textureEditorVisible;
                UpdateTextureEditor();
            };
            propertiesGrid.Widgets.Add(textureEditorButton);
            Grid.SetRow(textureEditorButton, 9);
            Grid.SetColumn(textureEditorButton, 0);
            Grid.SetColumnSpan(textureEditorButton, 4);

            // Close button
            var closeButton = new Button { Content = new Label { Text = "Close" }, Width = 60 };
            closeButton.Click += (s, e) =>
            {
                _propertiesWindowVisible = false;
                _sectorPropertiesWindow.Visible = false;
            };
            propertiesGrid.Widgets.Add(closeButton);
            Grid.SetRow(closeButton, 10);
            Grid.SetColumn(closeButton, 1);

            _sectorPropertiesWindow.Content = propertiesGrid;
            _desktop.Widgets.Add(_sectorPropertiesWindow);

            // Hide the close button
            if (_sectorPropertiesWindow.CloseButton != null)
            {
                _sectorPropertiesWindow.CloseButton.Visible = false;
            }

            // Add event handlers for property changes
            _floorHeightTextBox.TextChanged += OnSectorPropertyChanged;
            _ceilingHeightTextBox.TextChanged += OnSectorPropertyChanged;
            _sectorLoTagTextBox.TextChanged += OnSectorPropertyChanged;
            _sectorHiTagTextBox.TextChanged += OnSectorPropertyChanged;
            _isLiftCheckBox.IsCheckedChanged += OnSectorPropertyChanged;
            _liftLowHeightTextBox.TextChanged += OnSectorPropertyChanged;
            _liftHighHeightTextBox.TextChanged += OnSectorPropertyChanged;
            _liftSpeedTextBox.TextChanged += OnSectorPropertyChanged;
        }


        private void SetupTextureEditorWindow()
        {
            _textureEditorWindow = new Window
            {
                Title = "Texture Editor",
                Left = 600,
                Top = 100,
                Width = 450,
                Height = 700,
                Visible = false
            };

            var scrollPane = new ScrollViewer();
            var textureGrid = new Grid
            {
                RowSpacing = 3,
                ColumnSpacing = 5
            };

            // Setup grid with many rows
            for (int i = 0; i < 50; i++)
            {
                textureGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            }

            textureGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            textureGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            textureGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            textureGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            textureGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            int row = 0;

            // Title
            var titleLabel = new Label { Text = "Texture & Material Editor", TextColor = Color.White };
            textureGrid.Widgets.Add(titleLabel);
            Grid.SetRow(titleLabel, row++);
            Grid.SetColumnSpan(titleLabel, 5);

            // === FLOOR SECTION ===
            var floorLabel = new Label { Text = "═══ FLOOR ═══", TextColor = Color.Yellow };
            textureGrid.Widgets.Add(floorLabel);
            Grid.SetRow(floorLabel, row++);
            Grid.SetColumnSpan(floorLabel, 5);

            // Floor texture
            _floorTextureButtonTex = AddTextureRow(textureGrid, "Texture:", "Gray", "floor", ref row);

            // Floor UV Offset (Panning)
            (_floorUVOffsetXBox, _floorUVOffsetYBox) = AddUVOffsetRow(textureGrid, "UV Offset:", "floor", ref row);

            // Floor UV Scale
            (_floorUVScaleXBox, _floorUVScaleYBox) = AddUVScaleRow(textureGrid, "UV Scale:", "floor", ref row);

            // Floor UV Rotation
            _floorUVRotationBox = AddUVRotationRow(textureGrid, "UV Rotation:", "floor", ref row);

            // Floor UV Flip
            (_floorUVFlipXCheck, _floorUVFlipYCheck) = AddUVFlipRow(textureGrid, "UV Flip:", ref row);

            // Floor Shading
            _floorShadingBox = AddShadingRow(textureGrid, "Shading:", "floor", ref row);

            // === CEILING SECTION ===
            var ceilingLabel = new Label { Text = "═══ CEILING ═══", TextColor = Color.Cyan };
            textureGrid.Widgets.Add(ceilingLabel);
            Grid.SetRow(ceilingLabel, row++);
            Grid.SetColumnSpan(ceilingLabel, 5);

            // Ceiling texture
            _ceilingTextureButtonTex = AddTextureRow(textureGrid, "Texture:", "LightGray", "ceiling", ref row);
            (_ceilingUVOffsetXBox, _ceilingUVOffsetYBox) =
                AddUVOffsetRow(textureGrid, "UV Offset:", "ceiling", ref row);
            (_ceilingUVScaleXBox, _ceilingUVScaleYBox) = AddUVScaleRow(textureGrid, "UV Scale:", "ceiling", ref row);
            _ceilingUVRotationBox = AddUVRotationRow(textureGrid, "UV Rotation:", "ceiling", ref row);
            (_ceilingUVFlipXCheck, _ceilingUVFlipYCheck) = AddUVFlipRow(textureGrid, "UV Flip:", ref row);
            _ceilingShadingBox = AddShadingRow(textureGrid, "Shading:", "ceiling", ref row);

            // === WALL SECTION ===
            var wallLabel = new Label { Text = "═══ WALLS ═══", TextColor = Color.Orange };
            textureGrid.Widgets.Add(wallLabel);
            Grid.SetRow(wallLabel, row++);
            Grid.SetColumnSpan(wallLabel, 5);

            // Wall texture
            _wallTextureButtonTex = AddTextureRow(textureGrid, "Texture:", "White", "wall", ref row);
            (_wallUVOffsetXBox, _wallUVOffsetYBox) = AddUVOffsetRow(textureGrid, "UV Offset:", "wall", ref row);
            (_wallUVScaleXBox, _wallUVScaleYBox) = AddUVScaleRow(textureGrid, "UV Scale:", "wall", ref row);
            _wallUVRotationBox = AddUVRotationRow(textureGrid, "UV Rotation:", "wall", ref row);
            (_wallUVFlipXCheck, _wallUVFlipYCheck) = AddUVFlipRow(textureGrid, "UV Flip:", ref row);
            _wallShadingBox = AddShadingRow(textureGrid, "Shading:", "wall", ref row);

            // Close button
            var closeButton = new Button { Content = new Label { Text = "Close" }, Width = 80 };
            closeButton.Click += (s, e) =>
            {
                _textureEditorVisible = false;
                _textureEditorWindow.Visible = false;
            };
            textureGrid.Widgets.Add(closeButton);
            Grid.SetRow(closeButton, row);
            Grid.SetColumn(closeButton, 1);

            _textureEditorWindow.Content = scrollPane;
            scrollPane.Content = textureGrid;
            _desktop.Widgets.Add(_textureEditorWindow);

            // Hide the close button
            if (_textureEditorWindow.CloseButton != null)
            {
                _textureEditorWindow.CloseButton.Visible = false;
            }
        }

        private void SetupSpriteEditorWindow()
        {
            _spriteEditorWindow = new Window
            {
                Title = "Sprite Editor",
                Left = 1050,
                Top = 100,
                Width = 400,
                Height = 500,
                Visible = false
            };

            var scrollPane = new ScrollViewer();
            var spriteGrid = new Grid
            {
                RowSpacing = 5,
                ColumnSpacing = 8
            };

            // Setup grid with enough rows
            for (int i = 0; i < 20; i++)
            {
                spriteGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            }

            spriteGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            spriteGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

            int row = 0;

            // Title
            var titleLabel = new Label { Text = "Sprite Properties", TextColor = Color.White };
            spriteGrid.Widgets.Add(titleLabel);
            Grid.SetRow(titleLabel, row++);
            Grid.SetColumnSpan(titleLabel, 2);

            // Position
            var positionLabel = new Label { Text = "Position:", TextColor = Color.Yellow };
            spriteGrid.Widgets.Add(positionLabel);
            Grid.SetRow(positionLabel, row);

            var positionPanel = new Panel();
            _spritePositionXBox = new TextBox { Width = 80, Text = "0.0" };
            _spritePositionYBox = new TextBox { Width = 80, Text = "0.0", Left = 90 };
            var xLabel = new Label { Text = "X", TextColor = Color.White, Top = -20 };
            var yLabel = new Label { Text = "Y", TextColor = Color.White, Left = 90, Top = -20 };
            positionPanel.Widgets.Add(xLabel);
            positionPanel.Widgets.Add(yLabel);
            positionPanel.Widgets.Add(_spritePositionXBox);
            positionPanel.Widgets.Add(_spritePositionYBox);

            spriteGrid.Widgets.Add(positionPanel);
            Grid.SetRow(positionPanel, row);
            Grid.SetColumn(positionPanel, 1);
            row++;

            // Angle
            var angleLabel = new Label { Text = "Angle:", TextColor = Color.Yellow };
            spriteGrid.Widgets.Add(angleLabel);
            Grid.SetRow(angleLabel, row);

            _spriteAngleBox = new TextBox { Width = 100, Text = "0.0" };
            spriteGrid.Widgets.Add(_spriteAngleBox);
            Grid.SetRow(_spriteAngleBox, row);
            Grid.SetColumn(_spriteAngleBox, 1);
            row++;

            // Scale
            var scaleLabel = new Label { Text = "Scale:", TextColor = Color.Yellow };
            spriteGrid.Widgets.Add(scaleLabel);
            Grid.SetRow(scaleLabel, row);

            var scalePanel = new Panel();
            _spriteScaleXBox = new TextBox { Width = 80, Text = "1.0" };
            _spriteScaleYBox = new TextBox { Width = 80, Text = "1.0", Left = 90 };
            var scaleXLabel = new Label { Text = "X", TextColor = Color.White, Top = -20 };
            var scaleYLabel = new Label { Text = "Y", TextColor = Color.White, Left = 90, Top = -20 };
            scalePanel.Widgets.Add(scaleXLabel);
            scalePanel.Widgets.Add(scaleYLabel);
            scalePanel.Widgets.Add(_spriteScaleXBox);
            scalePanel.Widgets.Add(_spriteScaleYBox);

            spriteGrid.Widgets.Add(scalePanel);
            Grid.SetRow(scalePanel, row);
            Grid.SetColumn(scalePanel, 1);
            row++;

            // Palette
            var paletteLabel = new Label { Text = "Palette:", TextColor = Color.Yellow };
            spriteGrid.Widgets.Add(paletteLabel);
            Grid.SetRow(paletteLabel, row);

            _spritePaletteBox = new TextBox { Width = 100, Text = "0" };
            spriteGrid.Widgets.Add(_spritePaletteBox);
            Grid.SetRow(_spritePaletteBox, row);
            Grid.SetColumn(_spritePaletteBox, 1);
            row++;

            // Alignment
            var alignmentLabel = new Label { Text = "Alignment:", TextColor = Color.Yellow };
            spriteGrid.Widgets.Add(alignmentLabel);
            Grid.SetRow(alignmentLabel, row);

            _spriteAlignmentBox = new ComboView { Width = 120 };
            _spriteAlignmentBox.Widgets.Add(new Label { Text = "Floor", TextColor = Color.White });
            _spriteAlignmentBox.Widgets.Add(new Label { Text = "Wall", TextColor = Color.White });
            _spriteAlignmentBox.Widgets.Add(new Label { Text = "Face", TextColor = Color.White });
            _spriteAlignmentBox.SelectedIndex = 2; // Default to Face

            spriteGrid.Widgets.Add(_spriteAlignmentBox);
            Grid.SetRow(_spriteAlignmentBox, row);
            Grid.SetColumn(_spriteAlignmentBox, 1);
            row++;

            // Tag
            var tagLabel = new Label { Text = "Tag:", TextColor = Color.Yellow };
            spriteGrid.Widgets.Add(tagLabel);
            Grid.SetRow(tagLabel, row);

            _spriteTagBox = new ComboView { Width = 120 };
            _spriteTagBox.Widgets.Add(new Label { Text = "Decoration", TextColor = Color.White });
            _spriteTagBox.Widgets.Add(new Label { Text = "Switch", TextColor = Color.Orange });
            _spriteTagBox.SelectedIndex = 0; // Default to Decoration

            spriteGrid.Widgets.Add(_spriteTagBox);
            Grid.SetRow(_spriteTagBox, row);
            Grid.SetColumn(_spriteTagBox, 1);
            row++;

            // LoTag
            var spriteLoTagLabel = new Label { Text = "LoTag:", TextColor = Color.Yellow };
            spriteGrid.Widgets.Add(spriteLoTagLabel);
            Grid.SetRow(spriteLoTagLabel, row);

            _spriteLoTagBox = new TextBox { Width = 60, Text = "0" };
            spriteGrid.Widgets.Add(_spriteLoTagBox);
            Grid.SetRow(_spriteLoTagBox, row);
            Grid.SetColumn(_spriteLoTagBox, 1);
            row++;

            // HiTag  
            var spriteHiTagLabel = new Label { Text = "HiTag:", TextColor = Color.Yellow };
            spriteGrid.Widgets.Add(spriteHiTagLabel);
            Grid.SetRow(spriteHiTagLabel, row);

            _spriteHiTagBox = new TextBox { Width = 60, Text = "0" };
            spriteGrid.Widgets.Add(_spriteHiTagBox);
            Grid.SetRow(_spriteHiTagBox, row);
            Grid.SetColumn(_spriteHiTagBox, 1);
            row++;

            // Event handlers for property changes
            _spritePositionXBox.TextChanged += OnSpritePropertyChanged;
            _spritePositionYBox.TextChanged += OnSpritePropertyChanged;
            _spriteAngleBox.TextChanged += OnSpritePropertyChanged;
            _spriteScaleXBox.TextChanged += OnSpritePropertyChanged;
            _spriteScaleYBox.TextChanged += OnSpritePropertyChanged;
            _spritePaletteBox.TextChanged += OnSpritePropertyChanged;
            _spriteAlignmentBox.SelectedIndexChanged += OnSpritePropertyChanged;
            _spriteTagBox.SelectedIndexChanged += OnSpritePropertyChanged;
            _spriteLoTagBox.TextChanged += OnSpritePropertyChanged;
            _spriteHiTagBox.TextChanged += OnSpritePropertyChanged;

            scrollPane.Content = spriteGrid;
            _spriteEditorWindow.Content = scrollPane;
            _desktop.Widgets.Add(_spriteEditorWindow);

            // Hide the close button
            if (_spriteEditorWindow.CloseButton != null)
            {
                _spriteEditorWindow.CloseButton.Visible = false;
            }
        }

        private Button AddTextureRow(Grid grid, string label, string defaultTexture, string textureType, ref int row)
        {
            var labelWidget = new Label { Text = label, TextColor = Color.White };
            grid.Widgets.Add(labelWidget);
            Grid.SetRow(labelWidget, row);
            Grid.SetColumn(labelWidget, 0);

            var textureButton = new Button { Content = new Label { Text = defaultTexture }, Width = 100 };
            textureButton.Click += (s, e) => ShowTextureDropdown(textureType, textureButton);
            grid.Widgets.Add(textureButton);
            Grid.SetRow(textureButton, row);
            Grid.SetColumn(textureButton, 1);

            row++;
            return textureButton;
        }

        private (TextBox, TextBox) AddUVOffsetRow(Grid grid, string label, string textureType, ref int row)
        {
            var labelWidget = new Label { Text = label, TextColor = Color.White };
            grid.Widgets.Add(labelWidget);
            Grid.SetRow(labelWidget, row);
            Grid.SetColumn(labelWidget, 0);

            var offsetXBox = new TextBox { Text = "0.0", Width = 60 };
            offsetXBox.TextChanged += (s, e) => OnUVOffsetChanged(textureType, "X", offsetXBox.Text);
            grid.Widgets.Add(offsetXBox);
            Grid.SetRow(offsetXBox, row);
            Grid.SetColumn(offsetXBox, 1);

            var minusXBtn = new Button { Content = new Label { Text = "-" }, Width = 25 };
            minusXBtn.Click += (s, e) => AdjustUVOffset(textureType, "X", -0.1f);
            grid.Widgets.Add(minusXBtn);
            Grid.SetRow(minusXBtn, row);
            Grid.SetColumn(minusXBtn, 2);

            var plusXBtn = new Button { Content = new Label { Text = "+" }, Width = 25 };
            plusXBtn.Click += (s, e) => AdjustUVOffset(textureType, "X", 0.1f);
            grid.Widgets.Add(plusXBtn);
            Grid.SetRow(plusXBtn, row);
            Grid.SetColumn(plusXBtn, 3);

            row++;

            // Y offset row
            var yLabelWidget = new Label { Text = "Y:", TextColor = Color.White };
            grid.Widgets.Add(yLabelWidget);
            Grid.SetRow(yLabelWidget, row);
            Grid.SetColumn(yLabelWidget, 0);

            var offsetYBox = new TextBox { Text = "0.0", Width = 60 };
            offsetYBox.TextChanged += (s, e) => OnUVOffsetChanged(textureType, "Y", offsetYBox.Text);
            grid.Widgets.Add(offsetYBox);
            Grid.SetRow(offsetYBox, row);
            Grid.SetColumn(offsetYBox, 1);

            var minusYBtn = new Button { Content = new Label { Text = "-" }, Width = 25 };
            minusYBtn.Click += (s, e) => AdjustUVOffset(textureType, "Y", -0.1f);
            grid.Widgets.Add(minusYBtn);
            Grid.SetRow(minusYBtn, row);
            Grid.SetColumn(minusYBtn, 2);

            var plusYBtn = new Button { Content = new Label { Text = "+" }, Width = 25 };
            plusYBtn.Click += (s, e) => AdjustUVOffset(textureType, "Y", 0.1f);
            grid.Widgets.Add(plusYBtn);
            Grid.SetRow(plusYBtn, row);
            Grid.SetColumn(plusYBtn, 3);

            row++;
            return (offsetXBox, offsetYBox);
        }

        private (TextBox, TextBox) AddUVScaleRow(Grid grid, string label, string textureType, ref int row)
        {
            var labelWidget = new Label { Text = label, TextColor = Color.White };
            grid.Widgets.Add(labelWidget);
            Grid.SetRow(labelWidget, row);
            Grid.SetColumn(labelWidget, 0);

            var scaleXBox = new TextBox { Text = "1.0", Width = 60 };
            scaleXBox.TextChanged += (s, e) => OnUVScaleChanged(textureType, "X", scaleXBox.Text);
            grid.Widgets.Add(scaleXBox);
            Grid.SetRow(scaleXBox, row);
            Grid.SetColumn(scaleXBox, 1);

            var minusXBtn = new Button { Content = new Label { Text = "-" }, Width = 25 };
            minusXBtn.Click += (s, e) => AdjustUVScale(textureType, "X", -0.1f);
            grid.Widgets.Add(minusXBtn);
            Grid.SetRow(minusXBtn, row);
            Grid.SetColumn(minusXBtn, 2);

            var plusXBtn = new Button { Content = new Label { Text = "+" }, Width = 25 };
            plusXBtn.Click += (s, e) => AdjustUVScale(textureType, "X", 0.1f);
            grid.Widgets.Add(plusXBtn);
            Grid.SetRow(plusXBtn, row);
            Grid.SetColumn(plusXBtn, 3);

            row++;

            // Y scale row
            var yLabelWidget = new Label { Text = "Y:", TextColor = Color.White };
            grid.Widgets.Add(yLabelWidget);
            Grid.SetRow(yLabelWidget, row);
            Grid.SetColumn(yLabelWidget, 0);

            var scaleYBox = new TextBox { Text = "1.0", Width = 60 };
            scaleYBox.TextChanged += (s, e) => OnUVScaleChanged(textureType, "Y", scaleYBox.Text);
            grid.Widgets.Add(scaleYBox);
            Grid.SetRow(scaleYBox, row);
            Grid.SetColumn(scaleYBox, 1);

            var minusYBtn = new Button { Content = new Label { Text = "-" }, Width = 25 };
            minusYBtn.Click += (s, e) => AdjustUVScale(textureType, "Y", -0.1f);
            grid.Widgets.Add(minusYBtn);
            Grid.SetRow(minusYBtn, row);
            Grid.SetColumn(minusYBtn, 2);

            var plusYBtn = new Button { Content = new Label { Text = "+" }, Width = 25 };
            plusYBtn.Click += (s, e) => AdjustUVScale(textureType, "Y", 0.1f);
            grid.Widgets.Add(plusYBtn);
            Grid.SetRow(plusYBtn, row);
            Grid.SetColumn(plusYBtn, 3);

            row++;
            return (scaleXBox, scaleYBox);
        }

        private TextBox AddUVRotationRow(Grid grid, string label, string textureType, ref int row)
        {
            var labelWidget = new Label { Text = label, TextColor = Color.White };
            grid.Widgets.Add(labelWidget);
            Grid.SetRow(labelWidget, row);
            Grid.SetColumn(labelWidget, 0);

            var rotationBox = new TextBox { Text = "0.0", Width = 60 };
            rotationBox.TextChanged += (s, e) => OnUVRotationChanged(textureType, rotationBox.Text);
            grid.Widgets.Add(rotationBox);
            Grid.SetRow(rotationBox, row);
            Grid.SetColumn(rotationBox, 1);

            var minusBtn = new Button { Content = new Label { Text = "-" }, Width = 25 };
            minusBtn.Click += (s, e) => AdjustUVRotation(textureType, -15f);
            grid.Widgets.Add(minusBtn);
            Grid.SetRow(minusBtn, row);
            Grid.SetColumn(minusBtn, 2);

            var plusBtn = new Button { Content = new Label { Text = "+" }, Width = 25 };
            plusBtn.Click += (s, e) => AdjustUVRotation(textureType, 15f);
            grid.Widgets.Add(plusBtn);
            Grid.SetRow(plusBtn, row);
            Grid.SetColumn(plusBtn, 3);

            row++;
            return rotationBox;
        }

        private (CheckButton, CheckButton) AddUVFlipRow(Grid grid, string label, ref int row)
        {
            var labelWidget = new Label { Text = label, TextColor = Color.White };
            grid.Widgets.Add(labelWidget);
            Grid.SetRow(labelWidget, row);
            Grid.SetColumn(labelWidget, 0);

            var flipXPanel = new Panel();
            var flipXCheck = new CheckButton();
            var flipXLabel = new Label { Text = "X", TextColor = Color.White };
            flipXPanel.Widgets.Add(flipXCheck);
            flipXPanel.Widgets.Add(flipXLabel);
            grid.Widgets.Add(flipXPanel);
            Grid.SetRow(flipXPanel, row);
            Grid.SetColumn(flipXPanel, 1);

            var flipYPanel = new Panel();
            var flipYCheck = new CheckButton();
            var flipYLabel = new Label { Text = "Y", TextColor = Color.White };
            flipYPanel.Widgets.Add(flipYCheck);
            flipYPanel.Widgets.Add(flipYLabel);
            grid.Widgets.Add(flipYPanel);
            Grid.SetRow(flipYPanel, row);
            Grid.SetColumn(flipYPanel, 2);

            row++;
            return (flipXCheck, flipYCheck);
        }

        private TextBox AddShadingRow(Grid grid, string label, string textureType, ref int row)
        {
            var labelWidget = new Label { Text = label, TextColor = Color.White };
            grid.Widgets.Add(labelWidget);
            Grid.SetRow(labelWidget, row);
            Grid.SetColumn(labelWidget, 0);

            var shadingBox = new TextBox { Text = "1.0", Width = 60 };
            shadingBox.TextChanged += (s, e) => OnShadingChanged(textureType, shadingBox.Text);
            grid.Widgets.Add(shadingBox);
            Grid.SetRow(shadingBox, row);
            Grid.SetColumn(shadingBox, 1);

            var minusBtn = new Button { Content = new Label { Text = "-" }, Width = 25 };
            minusBtn.Click += (s, e) => AdjustShading(textureType, -0.1f);
            grid.Widgets.Add(minusBtn);
            Grid.SetRow(minusBtn, row);
            Grid.SetColumn(minusBtn, 2);

            var plusBtn = new Button { Content = new Label { Text = "+" }, Width = 25 };
            plusBtn.Click += (s, e) => AdjustShading(textureType, 0.1f);
            grid.Widgets.Add(plusBtn);
            Grid.SetRow(plusBtn, row);
            Grid.SetColumn(plusBtn, 3);

            row++;
            return shadingBox;
        }

        private void UpdateTextureEditor()
        {
            if (_selectedSector != null && _textureEditorVisible)
            {
                _textureEditorWindow.Visible = true;
                _textureEditorWindow.Title = $"Texture Editor - Sector {_selectedSector.Id}";

                // Update all texture controls with current values
                // Floor controls
                if (_floorTextureButtonTex != null)
                    ((Label)_floorTextureButtonTex.Content).Text = _selectedSector.FloorTexture;
                if (_floorUVOffsetXBox != null)
                    _floorUVOffsetXBox.Text = _selectedSector.FloorUVOffsetX.ToString("F2");
                if (_floorUVOffsetYBox != null)
                    _floorUVOffsetYBox.Text = _selectedSector.FloorUVOffsetY.ToString("F2");
                if (_floorUVScaleXBox != null)
                    _floorUVScaleXBox.Text = _selectedSector.FloorUVScaleX.ToString("F2");
                if (_floorUVScaleYBox != null)
                    _floorUVScaleYBox.Text = _selectedSector.FloorUVScaleY.ToString("F2");
                if (_floorUVRotationBox != null)
                    _floorUVRotationBox.Text = _selectedSector.FloorUVRotation.ToString("F1");
                if (_floorUVFlipXCheck != null)
                    _floorUVFlipXCheck.IsPressed = _selectedSector.FloorUVFlipX;
                if (_floorUVFlipYCheck != null)
                    _floorUVFlipYCheck.IsPressed = _selectedSector.FloorUVFlipY;
                if (_floorShadingBox != null)
                    _floorShadingBox.Text = _selectedSector.FloorShading.ToString("F2");

                // Ceiling controls
                if (_ceilingTextureButtonTex != null)
                    ((Label)_ceilingTextureButtonTex.Content).Text = _selectedSector.CeilingTexture;
                if (_ceilingUVOffsetXBox != null)
                    _ceilingUVOffsetXBox.Text = _selectedSector.CeilingUVOffsetX.ToString("F2");
                if (_ceilingUVOffsetYBox != null)
                    _ceilingUVOffsetYBox.Text = _selectedSector.CeilingUVOffsetY.ToString("F2");
                if (_ceilingUVScaleXBox != null)
                    _ceilingUVScaleXBox.Text = _selectedSector.CeilingUVScaleX.ToString("F2");
                if (_ceilingUVScaleYBox != null)
                    _ceilingUVScaleYBox.Text = _selectedSector.CeilingUVScaleY.ToString("F2");
                if (_ceilingUVRotationBox != null)
                    _ceilingUVRotationBox.Text = _selectedSector.CeilingUVRotation.ToString("F1");
                if (_ceilingUVFlipXCheck != null)
                    _ceilingUVFlipXCheck.IsPressed = _selectedSector.CeilingUVFlipX;
                if (_ceilingUVFlipYCheck != null)
                    _ceilingUVFlipYCheck.IsPressed = _selectedSector.CeilingUVFlipY;
                if (_ceilingShadingBox != null)
                    _ceilingShadingBox.Text = _selectedSector.CeilingShading.ToString("F2");

                // Wall controls
                if (_wallTextureButtonTex != null)
                    ((Label)_wallTextureButtonTex.Content).Text = _selectedSector.WallTexture;
                if (_wallUVOffsetXBox != null)
                    _wallUVOffsetXBox.Text = _selectedSector.WallUVOffsetX.ToString("F2");
                if (_wallUVOffsetYBox != null)
                    _wallUVOffsetYBox.Text = _selectedSector.WallUVOffsetY.ToString("F2");
                if (_wallUVScaleXBox != null)
                    _wallUVScaleXBox.Text = _selectedSector.WallUVScaleX.ToString("F2");
                if (_wallUVScaleYBox != null)
                    _wallUVScaleYBox.Text = _selectedSector.WallUVScaleY.ToString("F2");
                if (_wallUVRotationBox != null)
                    _wallUVRotationBox.Text = _selectedSector.WallUVRotation.ToString("F1");
                if (_wallUVFlipXCheck != null)
                    _wallUVFlipXCheck.IsPressed = _selectedSector.WallUVFlipX;
                if (_wallUVFlipYCheck != null)
                    _wallUVFlipYCheck.IsPressed = _selectedSector.WallUVFlipY;
                if (_wallShadingBox != null)
                    _wallShadingBox.Text = _selectedSector.WallShading.ToString("F2");
            }
            else
            {
                _textureEditorWindow.Visible = false;
            }
        }

        private void UpdateSectorPropertiesPanel()
        {
            if (_selectedSector != null && _propertiesWindowVisible)
            {
                _sectorPropertiesWindow.Visible = true;

                // Show if sector is nested
                var typeInfo = _selectedSector.IsNested ? " (Nested)" : " (Independent)";
                if (_selectedSector.IsNested && _selectedSector.ParentSectorId.HasValue)
                {
                    typeInfo += $" - Parent: {_selectedSector.ParentSectorId.Value}";
                }

                _sectorIdLabel.Text = $"Sector {_selectedSector.Id}{typeInfo}";

                _floorHeightTextBox.Text = _selectedSector.FloorHeight.ToString("F1");
                _ceilingHeightTextBox.Text = _selectedSector.CeilingHeight.ToString("F1");
                _sectorLoTagTextBox.Text = _selectedSector.LoTag.ToString();
                _sectorHiTagTextBox.Text = _selectedSector.HiTag.ToString();
                _isLiftCheckBox.IsChecked = _selectedSector.IsLift;
                _liftLowHeightTextBox.Text = _selectedSector.LiftLowHeight.ToString("F1");
                _liftHighHeightTextBox.Text = _selectedSector.LiftHighHeight.ToString("F1");
                _liftSpeedTextBox.Text = _selectedSector.LiftSpeed.ToString("F1");
                // Texture info will be handled by texture editor
            }
            else
            {
                _sectorPropertiesWindow.Visible = false;
            }
        }


        private void UpdateSpriteEditor()
        {
            if (_selectedSprite != null && _spriteEditorVisible)
            {
                _spriteEditorWindow.Visible = true;
                _spriteEditorWindow.Title = $"Sprite Editor - Sprite {_selectedSprite.Id}";

                // Disable event handlers while updating UI
                _isUpdatingSpriteUI = true;

                // Update all sprite controls with current values
                if (_spritePositionXBox != null)
                    _spritePositionXBox.Text = _selectedSprite.Position.X.ToString("F1");
                if (_spritePositionYBox != null)
                    _spritePositionYBox.Text = _selectedSprite.Position.Y.ToString("F1");
                if (_spriteAngleBox != null)
                    _spriteAngleBox.Text = _selectedSprite.Angle.ToString("F1");
                if (_spriteScaleXBox != null)
                    _spriteScaleXBox.Text = _selectedSprite.Scale.X.ToString("F2");
                if (_spriteScaleYBox != null)
                    _spriteScaleYBox.Text = _selectedSprite.Scale.Y.ToString("F2");
                if (_spritePaletteBox != null)
                    _spritePaletteBox.Text = _selectedSprite.Palette.ToString();
                if (_spriteAlignmentBox != null)
                    _spriteAlignmentBox.SelectedIndex = (int)_selectedSprite.Alignment;
                if (_spriteTagBox != null)
                    _spriteTagBox.SelectedIndex = (int)_selectedSprite.Tag;
                if (_spriteLoTagBox != null)
                    _spriteLoTagBox.Text = _selectedSprite.LoTag.ToString();
                if (_spriteHiTagBox != null)
                    _spriteHiTagBox.Text = _selectedSprite.HiTag.ToString();

                // Re-enable event handlers
                _isUpdatingSpriteUI = false;
            }
            else
            {
                // Always hide the window when sprite not selected or editor not visible
                if (_spriteEditorWindow != null)
                    _spriteEditorWindow.Visible = false;
            }
        }

        private void OnSectorPropertyChanged(object sender, EventArgs args)
        {
            if (_selectedSector == null) return;

            // Update sector properties from UI
            bool heightChanged = false;

            // Preserve nested sector properties
            bool wasNested = _selectedSector.IsNested;
            int? parentId = _selectedSector.ParentSectorId;

            if (float.TryParse(_floorHeightTextBox.Text, out float floorHeight))
            {
                if (Math.Abs(_selectedSector.FloorHeight - floorHeight) > 0.01f)
                {
                    _selectedSector.FloorHeight = floorHeight;
                    heightChanged = true;
                }
            }

            if (float.TryParse(_ceilingHeightTextBox.Text, out float ceilingHeight))
            {
                if (Math.Abs(_selectedSector.CeilingHeight - ceilingHeight) > 0.01f)
                {
                    _selectedSector.CeilingHeight = ceilingHeight;
                    heightChanged = true;
                }
            }

            // Update height transition walls if this is a nested sector and heights changed
            if (heightChanged && _selectedSector.IsNested && _selectedSector.ParentSectorId.HasValue)
            {
                var parentSector = _sectors.FirstOrDefault(s => s.Id == _selectedSector.ParentSectorId.Value);
                if (parentSector != null)
                {
                    UpdateHeightTransitionWalls(_selectedSector, parentSector);
                }
            }

            if (int.TryParse(_sectorLoTagTextBox.Text, out int loTag))
                _selectedSector.LoTag = loTag;

            if (int.TryParse(_sectorHiTagTextBox.Text, out int hiTag))
                _selectedSector.HiTag = hiTag;

            // Update lift properties
            _selectedSector.IsLift = _isLiftCheckBox.IsChecked;

            if (float.TryParse(_liftLowHeightTextBox.Text, out float liftLowHeight))
                _selectedSector.LiftLowHeight = liftLowHeight;

            if (float.TryParse(_liftHighHeightTextBox.Text, out float liftHighHeight))
                _selectedSector.LiftHighHeight = liftHighHeight;

            if (float.TryParse(_liftSpeedTextBox.Text, out float liftSpeed))
                _selectedSector.LiftSpeed = liftSpeed;

            // Ensure nested sector properties are preserved
            if (wasNested)
            {
                _selectedSector.IsNested = true;
                _selectedSector.ParentSectorId = parentId;
            }
        }


        private void OnSpritePropertyChanged(object sender, EventArgs args)
        {
            if (_selectedSprite == null || _isUpdatingSpriteUI) return;

            // Update sprite properties from UI
            if (float.TryParse(_spritePositionXBox.Text, out float x))
                _selectedSprite.Position = new Vector2(x, _selectedSprite.Position.Y);

            if (float.TryParse(_spritePositionYBox.Text, out float y))
                _selectedSprite.Position = new Vector2(_selectedSprite.Position.X, y);

            if (float.TryParse(_spriteAngleBox.Text, out float angle))
                _selectedSprite.Angle = angle;

            if (float.TryParse(_spriteScaleXBox.Text, out float scaleX))
                _selectedSprite.Scale = new Vector2(scaleX, _selectedSprite.Scale.Y);

            if (float.TryParse(_spriteScaleYBox.Text, out float scaleY))
                _selectedSprite.Scale = new Vector2(_selectedSprite.Scale.X, scaleY);

            if (int.TryParse(_spritePaletteBox.Text, out int palette))
                _selectedSprite.Palette = palette;

            if (_spriteAlignmentBox.SelectedIndex >= 0 && _spriteAlignmentBox.SelectedIndex < 3)
            {
                _selectedSprite.Alignment = (SpriteAlignment)_spriteAlignmentBox.SelectedIndex;
            }

            if (_spriteTagBox.SelectedIndex >= 0 && _spriteTagBox.SelectedIndex < 2)
            {
                var oldTag = _selectedSprite.Tag;
                _selectedSprite.Tag = (SpriteTag)_spriteTagBox.SelectedIndex;

                // Auto-update LoTag based on tag functionality
                if (oldTag != _selectedSprite.Tag)
                {
                    switch (_selectedSprite.Tag)
                    {
                        case SpriteTag.Switch:
                            // Switches need LoTag to specify which doors/sectors to activate
                            if (_selectedSprite.LoTag == 0)
                            {
                                _selectedSprite.LoTag = 1; // Default switch activation tag
                                // Update UI to reflect the new LoTag
                                if (_spriteLoTagBox != null)
                                {
                                    _isUpdatingSpriteUI = true;
                                    _spriteLoTagBox.Text = _selectedSprite.LoTag.ToString();
                                    _isUpdatingSpriteUI = false;
                                }
                            }
                            break;
                        case SpriteTag.Decoration:
                            // Decorations don't need functional tags
                            break;
                    }
                }
            }

            if (_spriteLoTagBox != null && int.TryParse(_spriteLoTagBox.Text, out int spriteLoTag))
                _selectedSprite.LoTag = spriteLoTag;

            if (_spriteHiTagBox != null && int.TryParse(_spriteHiTagBox.Text, out int spriteHiTag))
                _selectedSprite.HiTag = spriteHiTag;

            // Ensure UI state remains consistent after property changes
            _isUpdatingSpriteUI = false;
        }

        // Tag validation and linking system methods
        public List<Sector> FindSectorsByLoTag(int loTag)
        {
            return _sectors.Where(s => s.LoTag == loTag).ToList();
        }

        public List<Sprite> FindSpritesByLoTag(int loTag)
        {
            return _sectors.SelectMany(s => s.Sprites).Where(sp => sp.LoTag == loTag).ToList();
        }

        public List<Sector> FindSectorsByHiTag(int hiTag)
        {
            return _sectors.Where(s => s.HiTag == hiTag).ToList();
        }

        public List<Sprite> FindSpritesByHiTag(int hiTag)
        {
            return _sectors.SelectMany(s => s.Sprites).Where(sp => sp.HiTag == hiTag).ToList();
        }

        public bool ValidateTagLink(int sourceHiTag, int targetId)
        {
            // Check if target exists (either sector or sprite with matching ID)
            var targetSector = _sectors.FirstOrDefault(s => s.Id == targetId);
            var targetSprite = _sectors.SelectMany(s => s.Sprites).FirstOrDefault(sp => sp.Id == targetId);

            return targetSector != null || targetSprite != null;
        }

        public string GetTagBehaviorDescription(int loTag, bool isSprite = false)
        {
            if (isSprite)
            {
                return loTag switch
                {
                    TagConstants.SPRITE_DECORATION => "Decoration",
                    TagConstants.SPRITE_ENEMY => "Enemy",
                    TagConstants.SPRITE_PICKUP => "Pickup",
                    TagConstants.SPRITE_TRIGGER => "Trigger",
                    TagConstants.SPRITE_LIGHT => "Light Source",
                    TagConstants.SPRITE_SOUND => "Sound Source",
                    TagConstants.SPRITE_SWITCH => "Switch/Button",
                    TagConstants.SPRITE_TELEPORTER_EXIT => "Teleporter Exit",
                    _ => $"Custom Behavior ({loTag})"
                };
            }
            else
            {
                return loTag switch
                {
                    TagConstants.SECTOR_NORMAL => "Normal",
                    TagConstants.SECTOR_LIFT => "Lift/Elevator",
                    TagConstants.SECTOR_WATER => "Water/Liquid",
                    TagConstants.SECTOR_DAMAGE => "Damage Zone",
                    TagConstants.SECTOR_TELEPORTER => "Teleporter",
                    _ => $"Custom Behavior ({loTag})"
                };
            }
        }


        private Wall GetWallUnderCursor(Vector2 cursorPos, float tolerance)
        {
            foreach (var sector in _sectors)
            {
                foreach (var wall in sector.Walls)
                {
                    float distance = DistanceToLineSegment(cursorPos, wall.Start, wall.End);
                    if (distance <= tolerance)
                    {
                        return wall;
                    }
                }
            }

            return null;
        }


        private void AdjustFloorHeight(float amount)
        {
            if (_selectedSector == null) return;

            _selectedSector.FloorHeight += amount;
            _floorHeightTextBox.Text = _selectedSector.FloorHeight.ToString("F1");

            // Update height transition walls if this is a nested sector
            if (_selectedSector.IsNested && _selectedSector.ParentSectorId.HasValue)
            {
                var parentSector = _sectors.FirstOrDefault(s => s.Id == _selectedSector.ParentSectorId.Value);
                if (parentSector != null)
                {
                    UpdateHeightTransitionWalls(_selectedSector, parentSector);
                }
            }
        }

        private void AdjustCeilingHeight(float amount)
        {
            if (_selectedSector == null) return;

            _selectedSector.CeilingHeight += amount;
            _ceilingHeightTextBox.Text = _selectedSector.CeilingHeight.ToString("F1");

            // Update height transition walls if this is a nested sector
            if (_selectedSector.IsNested && _selectedSector.ParentSectorId.HasValue)
            {
                var parentSector = _sectors.FirstOrDefault(s => s.Id == _selectedSector.ParentSectorId.Value);
                if (parentSector != null)
                {
                    UpdateHeightTransitionWalls(_selectedSector, parentSector);
                }
            }
        }



        private void UpdateLiftAnimation(Sector lift, float deltaTime)
        {
            if (!lift.IsLift) return;

            float heightDifference = lift.LiftHighHeight - lift.LiftLowHeight;
            float moveDistance = lift.LiftSpeed * deltaTime;

            switch (lift.LiftState)
            {
                case LiftState.Rising:
                    lift.AnimationHeightOffset += moveDistance;
                    if (lift.AnimationHeightOffset >= heightDifference)
                    {
                        lift.AnimationHeightOffset = heightDifference;
                        lift.LiftState = LiftState.AtTop;
                        lift.IsAnimating = false;
                        lift.PlayerWasStandingOnLift = false; // Reset when lift stops
                    }
                    else
                    {
                        lift.IsAnimating = true;
                    }

                    break;

                case LiftState.Lowering:
                    lift.AnimationHeightOffset -= moveDistance;
                    if (lift.AnimationHeightOffset <= 0)
                    {
                        lift.AnimationHeightOffset = 0;
                        lift.LiftState = LiftState.AtBottom;
                        lift.IsAnimating = false;
                        lift.PlayerWasStandingOnLift = false; // Reset when lift stops
                    }
                    else
                    {
                        lift.IsAnimating = true;
                    }

                    break;

                case LiftState.AtBottom:
                case LiftState.AtTop:
                    lift.IsAnimating = false;
                    break;
            }
        }


        private void ActivateNearbyElements()
        {
            // Use camera position for activation
            Vector2 activationPosition =
                _is3DMode ? new Vector2(_camera3DPosition.X, -_camera3DPosition.Z) : _camera.Position;

            // Find nearby switches and activate them
            foreach (var sector in _sectors)
            {
                foreach (var sprite in sector.Sprites.Where(s => s.Tag == SpriteTag.Switch))
                {
                    var distance = Vector2.Distance(activationPosition, sprite.Position);
                    if (distance <= 64f) // Activation distance
                    {
                        ActivateSwitch(sprite);
                    }
                }
            }
        }

        private void ActivateSwitch(Sprite sprite)
        {
            // Only switch sprites can activate doors
            if (sprite.Tag != SpriteTag.Switch) return;

            Console.WriteLine($"Activating switch with LoTag: {sprite.LoTag}");

            // Find doors with matching HiTag



            // Also check for sectors with matching HiTag (for lifts/elevators)
            var linkedSectors = _sectors.Where(sector => sector.HiTag == sprite.HiTag && sprite.HiTag != 0).ToList();
            Console.WriteLine($"Found {linkedSectors.Count} linked sectors");

            foreach (var sector in linkedSectors)
            {
                Console.WriteLine($"Activating sector with LoTag: {sector.LoTag}, IsLift: {sector.IsLift}");
                // Activate sector-based effects (like lifts)
                ActivateSectorEffect(sector);
            }
        }

        private void ActivateSectorEffect(Sector sector)
        {
            Console.WriteLine(
                $"ActivateSectorEffect called for sector LoTag: {sector.LoTag}, expected: {TagConstants.SECTOR_LIFT}");
            // Handle sector-based effects based on LoTag
            switch (sector.LoTag)
            {
                case TagConstants.SECTOR_LIFT:
                    Console.WriteLine($"Lift sector found, IsLift: {sector.IsLift}");
                    if (sector.IsLift)
                    {
                        Console.WriteLine($"Activating lift, current state: {sector.LiftState}");
                        // Toggle lift between bottom and top positions
                        switch (sector.LiftState)
                        {
                            case LiftState.AtBottom:
                                Console.WriteLine("Starting lift rising");
                                sector.LiftState = LiftState.Rising;
                                // Check if player is standing on this lift
                                sector.PlayerWasStandingOnLift = IsPlayerStandingOnSector(sector);
                                Console.WriteLine($"Lift activation: Sector {sector.Id} (IsNested={sector.IsNested}), PlayerStandingOnLift={sector.PlayerWasStandingOnLift}");
                                break;
                            case LiftState.AtTop:
                                Console.WriteLine("Starting lift lowering");
                                sector.LiftState = LiftState.Lowering;
                                // Check if player is standing on this lift
                                sector.PlayerWasStandingOnLift = IsPlayerStandingOnSector(sector);
                                Console.WriteLine($"Lift activation: Sector {sector.Id} (IsNested={sector.IsNested}), PlayerStandingOnLift={sector.PlayerWasStandingOnLift}");
                                break;
                            case LiftState.Rising:
                            case LiftState.Lowering:
                                // If already moving, reverse direction
                                sector.LiftState = sector.LiftState == LiftState.Rising
                                    ? LiftState.Lowering
                                    : LiftState.Rising;
                                Console.WriteLine($"Reversing lift direction to: {sector.LiftState}");
                                break;
                        }
                    }

                    break;
                case TagConstants.SECTOR_TELEPORTER:
                    // Teleporter activation could be implemented here
                    break;
            }
        }

        private void Update3DCursor(MouseState mouseState)
        {
            if (!_is3DMode) return;

            // Cast ray from mouse and find the first thing it hits
            var ray = ScreenPointToRay(mouseState.Position);
            PerformRayCollisionDetection(ray);

            // Auto-alignment: make selected sprite follow cursor when enabled
            // BUT disable auto-alignment during sprite mouse tracking mode (G key mode)
            if (_autoAlignMode && _selectedSprite3D != null && !_isDraggingGizmo && !_spriteMouseTrackMode)
            {
                CheckAndApplyAutoAlignment(_selectedSprite3D);
            }
        }

        private void PerformRayCollisionDetection(Ray ray)
        {
            float closestDistance = float.MaxValue;
            _cursor3DSnapType = "none";
            _cursor3DSector = null;
            _cursor3DWall = null;
            _hoveredSprite3D = null;


            // First, check all sprites for intersection (sprites have priority)
            // Skip sprite detection if auto-alignment is enabled and we have a selected sprite
            if (!(_autoAlignMode && _selectedSprite3D != null))
            {
                foreach (var sector in _sectors)
                {
                    foreach (var sprite in sector.Sprites)
                    {
                        // Skip the selected sprite when auto-alignment is enabled
                        if (_autoAlignMode && sprite == _selectedSprite3D)
                            continue;

                        var spriteWorldPos = Get3DSpriteWorldPosition(sprite);


                        // Much simpler approach: check distance from ray to sprite center
                        var toSprite = spriteWorldPos - ray.Position;
                        var rayLength = Vector3.Dot(toSprite, ray.Direction);

                        if (rayLength > 0) // Sprite is in front of camera
                        {
                            var pointOnRay = ray.Position + ray.Direction * rayLength;
                            var distanceToSprite = Vector3.Distance(pointOnRay, spriteWorldPos);

                            // Smaller detection radius for selection only
                            var detectionRadius = 16f;

                            if (distanceToSprite <= detectionRadius && rayLength < closestDistance)
                            {
                                closestDistance = rayLength;
                                _hoveredSprite3D = sprite;
                                _cursor3DSector = sector;

                                // Don't snap cursor to sprite, just mark it as hovered
                                _cursor3DSnapType = "none";
                            }
                        }
                    }
                }
            }


            // Check surfaces if no sprite is hovered, OR if auto-alignment is enabled
            if (_hoveredSprite3D == null && (_selectedSprite3D == null || _autoAlignMode))
            {
                foreach (var sector in _sectors)
                {
                    // Check floor intersection
                    var floorHeight = sector.FloorHeight + sector.AnimationHeightOffset;
                    if (RayIntersectsPlane(ray, new Vector3(0, floorHeight, 0), Vector3.Up, out Vector3 floorPoint,
                            out float floorDistance))
                    {
                        if (IsPointInSector(new Vector2(floorPoint.X, -floorPoint.Z), sector) &&
                            floorDistance < closestDistance)
                        {
                            closestDistance = floorDistance;
                            _cursor3DPosition = floorPoint;
                            _cursor3DNormal = Vector3.Up;
                            _cursor3DSnapType = "floor";
                            _cursor3DSector = sector;
                        }
                    }

                    // Check ceiling intersection
                    var ceilingHeight = sector.CeilingHeight + sector.AnimationHeightOffset;
                    if (RayIntersectsPlane(ray, new Vector3(0, ceilingHeight, 0), Vector3.Down,
                            out Vector3 ceilingPoint, out float ceilingDistance))
                    {
                        if (IsPointInSector(new Vector2(ceilingPoint.X, -ceilingPoint.Z), sector) &&
                            ceilingDistance < closestDistance)
                        {
                            closestDistance = ceilingDistance;
                            _cursor3DPosition = ceilingPoint;
                            _cursor3DNormal = Vector3.Down;
                            _cursor3DSnapType = "ceiling";
                            _cursor3DSector = sector;
                        }
                    }

                    // Check wall intersections
                    foreach (var wall in sector.Walls)
                    {
                        if (RayIntersectsWall(ray, wall, sector, out Vector3 wallPoint, out float wallDistance,
                                out Vector3 wallNormal))
                        {
                            if (wallDistance < closestDistance)
                            {
                                closestDistance = wallDistance;
                                _cursor3DPosition = wallPoint;
                                _cursor3DNormal = wallNormal;
                                _cursor3DSnapType = "wall";
                                _cursor3DSector = sector;
                                _cursor3DWall = wall;
                            }
                        }
                    }
                }
            }
        }

        private bool RayIntersectsBoundingBox(Ray ray, Vector3 min, Vector3 max, out float distance)
        {
            distance = 0;

            float tmin = (min.X - ray.Position.X) / ray.Direction.X;
            float tmax = (max.X - ray.Position.X) / ray.Direction.X;

            if (tmin > tmax)
            {
                var temp = tmin;
                tmin = tmax;
                tmax = temp;
            }

            float tymin = (min.Y - ray.Position.Y) / ray.Direction.Y;
            float tymax = (max.Y - ray.Position.Y) / ray.Direction.Y;

            if (tymin > tymax)
            {
                var temp = tymin;
                tymin = tymax;
                tymax = temp;
            }

            if (tmin > tymax || tymin > tmax) return false;

            if (tymin > tmin) tmin = tymin;
            if (tymax < tmax) tmax = tymax;

            float tzmin = (min.Z - ray.Position.Z) / ray.Direction.Z;
            float tzmax = (max.Z - ray.Position.Z) / ray.Direction.Z;

            if (tzmin > tzmax)
            {
                var temp = tzmin;
                tzmin = tzmax;
                tzmax = temp;
            }

            if (tmin > tzmax || tzmin > tmax) return false;

            if (tzmin > tmin) tmin = tzmin;
            if (tzmax < tmax) tmax = tzmax;

            distance = tmin > 0 ? tmin : tmax;
            return distance > 0;
        }

        private Ray ScreenPointToRay(Point screenPoint)
        {
            // Use the same viewport and matrices that are used for 3D rendering
            var viewportBounds = GetViewportBounds();
            var viewport = new Viewport(viewportBounds.X, viewportBounds.Y,
                viewportBounds.Width, viewportBounds.Height);

            // Use the same view and projection matrices as 3D rendering
            var world = Matrix.Identity;
            var view = Matrix.CreateLookAt(_camera3DPosition, _camera3DTarget, Vector3.Up);
            var aspectRatio = (float)viewportBounds.Width / viewportBounds.Height;
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1f, 1000f);

            // Convert screen coordinates to world space
            var nearPoint = viewport.Unproject(new Vector3(screenPoint.X, screenPoint.Y, 0), projection, view, world);
            var farPoint = viewport.Unproject(new Vector3(screenPoint.X, screenPoint.Y, 1), projection, view, world);

            var direction = Vector3.Normalize(farPoint - nearPoint);
            return new Ray(nearPoint, direction);
        }

        private void PerformRaycasting(Ray ray)
        {
            float closestDistance = float.MaxValue;
            _cursor3DSnapType = "none";
            _cursor3DSector = null;
            _cursor3DWall = null;

            foreach (var sector in _sectors)
            {
                // Check floor intersection
                var floorHeight = sector.FloorHeight + sector.AnimationHeightOffset;
                if (RayIntersectsPlane(ray, new Vector3(0, floorHeight, 0), Vector3.Up, out Vector3 floorPoint,
                        out float floorDistance))
                {
                    if (IsPointInSector(new Vector2(floorPoint.X, -floorPoint.Z), sector) &&
                        floorDistance < closestDistance)
                    {
                        closestDistance = floorDistance;
                        _cursor3DPosition = floorPoint;
                        _cursor3DNormal = Vector3.Up;
                        _cursor3DSnapType = "floor";
                        _cursor3DSector = sector;
                    }
                }

                // Check ceiling intersection
                if (RayIntersectsPlane(ray, new Vector3(0, sector.CeilingHeight, 0), Vector3.Down,
                        out Vector3 ceilingPoint, out float ceilingDistance))
                {
                    if (IsPointInSector(new Vector2(ceilingPoint.X, -ceilingPoint.Z), sector) &&
                        ceilingDistance < closestDistance)
                    {
                        closestDistance = ceilingDistance;
                        _cursor3DPosition = ceilingPoint;
                        _cursor3DNormal = Vector3.Down;
                        _cursor3DSnapType = "ceiling";
                        _cursor3DSector = sector;
                    }
                }

                // Check wall intersections
                foreach (var wall in sector.Walls)
                {
                    if (RayIntersectsWall(ray, wall, sector, out Vector3 wallPoint, out float wallDistance,
                            out Vector3 wallNormal))
                    {
                        if (wallDistance < closestDistance)
                        {
                            closestDistance = wallDistance;
                            _cursor3DPosition = wallPoint;
                            _cursor3DNormal = wallNormal;
                            _cursor3DSnapType = "wall";
                            _cursor3DSector = sector;
                            _cursor3DWall = wall;
                        }
                    }
                }
            }
        }

        private bool RayIntersectsPlane(Ray ray, Vector3 planePoint, Vector3 planeNormal, out Vector3 intersectionPoint,
            out float distance)
        {
            intersectionPoint = Vector3.Zero;
            distance = 0;

            float denominator = Vector3.Dot(planeNormal, ray.Direction);
            if (Math.Abs(denominator) < 0.0001f) return false; // Ray parallel to plane

            Vector3 planeToRay = planePoint - ray.Position;
            distance = Vector3.Dot(planeToRay, planeNormal) / denominator;

            if (distance < 0) return false; // Intersection behind ray origin

            intersectionPoint = ray.Position + ray.Direction * distance;
            return true;
        }

        private bool RayIntersectsWall(Ray ray, Wall wall, Sector sector, out Vector3 intersectionPoint,
            out float distance, out Vector3 normal)
        {
            intersectionPoint = Vector3.Zero;
            distance = 0;
            normal = Vector3.Zero;

            // Convert 2D wall to 3D
            var wallStart3D = new Vector3(wall.Start.X, 0, -wall.Start.Y);
            var wallEnd3D = new Vector3(wall.End.X, 0, -wall.End.Y);
            var wallDirection = Vector3.Normalize(wallEnd3D - wallStart3D);

            // Wall normal (perpendicular to wall direction, pointing inward to sector)
            // Calculate both possible normals
            Vector3 normal1 = Vector3.Cross(wallDirection, Vector3.Up);
            Vector3 normal2 = Vector3.Cross(Vector3.Up, wallDirection);

            // Test both normals to see which one points inside the sector
            Vector2 wallMidpoint2D = (wall.Start + wall.End) / 2f;

            // Test points slightly inward from wall using each normal
            Vector2 testPoint1 = wallMidpoint2D + new Vector2(normal1.X, normal1.Z) * 5f;
            Vector2 testPoint2 = wallMidpoint2D + new Vector2(normal2.X, normal2.Z) * 5f;

            // Use point-in-polygon test to see which point is inside the sector
            bool point1Inside = IsPointInSector(testPoint1, sector);
            bool point2Inside = IsPointInSector(testPoint2, sector);

            // Choose the normal whose test point is inside the sector
            // If both or neither are inside, fall back to camera direction
            if (point1Inside && !point2Inside)
            {
                normal = normal1;
            }
            else if (point2Inside && !point1Inside)
            {
                normal = normal2;
            }
            else
            {
                // Fallback: choose based on camera direction
                Vector3 wallMidpoint3D = (wallStart3D + wallEnd3D) / 2f;
                Vector3 toCamera = Vector3.Normalize(_camera3DPosition - wallMidpoint3D);
                float dot1 = Vector3.Dot(normal1, toCamera);
                float dot2 = Vector3.Dot(normal2, toCamera);
                normal = (dot1 > dot2) ? normal1 : normal2;
            }

            // Create plane from wall
            var floorHeight = sector.FloorHeight + sector.AnimationHeightOffset;
            var wallPlanePoint = new Vector3(wall.Start.X, floorHeight, -wall.Start.Y);

            if (!RayIntersectsPlane(ray, wallPlanePoint, normal, out intersectionPoint, out distance))
                return false;

            // Check if intersection point is within wall bounds (height and length)
            if (intersectionPoint.Y < floorHeight || intersectionPoint.Y > sector.CeilingHeight)
                return false;

            // Check if point is within wall segment
            var wallStart2D = new Vector2(wall.Start.X, -wall.Start.Y);
            var wallEnd2D = new Vector2(wall.End.X, -wall.End.Y);
            var intersect2D = new Vector2(intersectionPoint.X, intersectionPoint.Z);

            var wallVector = wallEnd2D - wallStart2D;
            var toIntersect = intersect2D - wallStart2D;
            var projection = Vector2.Dot(toIntersect, wallVector) / wallVector.LengthSquared();

            return projection >= 0 && projection <= 1;
        }

        private void CycleTexture(string textureType)
        {
            if (_selectedSector == null) return;

            var textureNames = _textureColors.Keys.ToList();
            string currentTexture;

            switch (textureType)
            {
                case "floor":
                    currentTexture = _selectedSector.FloorTexture;
                    break;
                case "ceiling":
                    currentTexture = _selectedSector.CeilingTexture;
                    break;
                case "wall":
                    currentTexture = _selectedSector.WallTexture;
                    break;
                default:
                    return;
            }

            var currentIndex = textureNames.IndexOf(currentTexture);
            var nextIndex = (currentIndex + 1) % textureNames.Count;
            var newTexture = textureNames[nextIndex];

            switch (textureType)
            {
                case "floor":
                    _selectedSector.FloorTexture = newTexture;
                    ((Label)_floorTextureButton.Content).Text = newTexture;
                    break;
                case "ceiling":
                    _selectedSector.CeilingTexture = newTexture;
                    ((Label)_ceilingTextureButton.Content).Text = newTexture;
                    break;
                case "wall":
                    _selectedSector.WallTexture = newTexture;
                    ((Label)_wallTextureButton.Content).Text = newTexture;
                    break;
            }
        }

        // UV Offset (Panning) Controls
        private void OnUVOffsetChanged(string textureType, string axis, string value)
        {
            if (_selectedSector == null || !float.TryParse(value, out float offsetValue)) return;

            switch (textureType.ToLower())
            {
                case "floor":
                    if (axis == "X") _selectedSector.FloorUVOffsetX = offsetValue;
                    else if (axis == "Y") _selectedSector.FloorUVOffsetY = offsetValue;
                    break;
                case "ceiling":
                    if (axis == "X") _selectedSector.CeilingUVOffsetX = offsetValue;
                    else if (axis == "Y") _selectedSector.CeilingUVOffsetY = offsetValue;
                    break;
                case "wall":
                    if (axis == "X") _selectedSector.WallUVOffsetX = offsetValue;
                    else if (axis == "Y") _selectedSector.WallUVOffsetY = offsetValue;
                    break;
            }
        }

        private void AdjustUVOffset(string textureType, string axis, float amount)
        {
            if (_selectedSector == null) return;

            switch (textureType.ToLower())
            {
                case "floor":
                    if (axis == "X")
                    {
                        _selectedSector.FloorUVOffsetX += amount;
                        if (_floorUVOffsetXBox != null)
                            _floorUVOffsetXBox.Text = _selectedSector.FloorUVOffsetX.ToString("F2");
                    }
                    else if (axis == "Y")
                    {
                        _selectedSector.FloorUVOffsetY += amount;
                        if (_floorUVOffsetYBox != null)
                            _floorUVOffsetYBox.Text = _selectedSector.FloorUVOffsetY.ToString("F2");
                    }

                    break;
                case "ceiling":
                    if (axis == "X")
                    {
                        _selectedSector.CeilingUVOffsetX += amount;
                        if (_ceilingUVOffsetXBox != null)
                            _ceilingUVOffsetXBox.Text = _selectedSector.CeilingUVOffsetX.ToString("F2");
                    }
                    else if (axis == "Y")
                    {
                        _selectedSector.CeilingUVOffsetY += amount;
                        if (_ceilingUVOffsetYBox != null)
                            _ceilingUVOffsetYBox.Text = _selectedSector.CeilingUVOffsetY.ToString("F2");
                    }

                    break;
                case "wall":
                    if (axis == "X")
                    {
                        _selectedSector.WallUVOffsetX += amount;
                        if (_wallUVOffsetXBox != null)
                            _wallUVOffsetXBox.Text = _selectedSector.WallUVOffsetX.ToString("F2");
                    }
                    else if (axis == "Y")
                    {
                        _selectedSector.WallUVOffsetY += amount;
                        if (_wallUVOffsetYBox != null)
                            _wallUVOffsetYBox.Text = _selectedSector.WallUVOffsetY.ToString("F2");
                    }

                    break;
            }
        }

        // UV Scale Controls
        private void OnUVScaleChanged(string textureType, string axis, string value)
        {
            if (_selectedSector == null || !float.TryParse(value, out float scaleValue)) return;
            if (scaleValue <= 0) scaleValue = 0.1f; // Prevent zero/negative scales

            switch (textureType.ToLower())
            {
                case "floor":
                    if (axis == "X") _selectedSector.FloorUVScaleX = scaleValue;
                    else if (axis == "Y") _selectedSector.FloorUVScaleY = scaleValue;
                    break;
                case "ceiling":
                    if (axis == "X") _selectedSector.CeilingUVScaleX = scaleValue;
                    else if (axis == "Y") _selectedSector.CeilingUVScaleY = scaleValue;
                    break;
                case "wall":
                    if (axis == "X") _selectedSector.WallUVScaleX = scaleValue;
                    else if (axis == "Y") _selectedSector.WallUVScaleY = scaleValue;
                    break;
            }
        }

        private void AdjustUVScale(string textureType, string axis, float amount)
        {
            if (_selectedSector == null) return;

            switch (textureType.ToLower())
            {
                case "floor":
                    if (axis == "X")
                    {
                        _selectedSector.FloorUVScaleX = Math.Max(0.1f, _selectedSector.FloorUVScaleX + amount);
                        if (_floorUVScaleXBox != null)
                            _floorUVScaleXBox.Text = _selectedSector.FloorUVScaleX.ToString("F2");
                    }
                    else if (axis == "Y")
                    {
                        _selectedSector.FloorUVScaleY = Math.Max(0.1f, _selectedSector.FloorUVScaleY + amount);
                        if (_floorUVScaleYBox != null)
                            _floorUVScaleYBox.Text = _selectedSector.FloorUVScaleY.ToString("F2");
                    }

                    break;
                case "ceiling":
                    if (axis == "X")
                    {
                        _selectedSector.CeilingUVScaleX = Math.Max(0.1f, _selectedSector.CeilingUVScaleX + amount);
                        if (_ceilingUVScaleXBox != null)
                            _ceilingUVScaleXBox.Text = _selectedSector.CeilingUVScaleX.ToString("F2");
                    }
                    else if (axis == "Y")
                    {
                        _selectedSector.CeilingUVScaleY = Math.Max(0.1f, _selectedSector.CeilingUVScaleY + amount);
                        if (_ceilingUVScaleYBox != null)
                            _ceilingUVScaleYBox.Text = _selectedSector.CeilingUVScaleY.ToString("F2");
                    }

                    break;
                case "wall":
                    if (axis == "X")
                    {
                        _selectedSector.WallUVScaleX = Math.Max(0.1f, _selectedSector.WallUVScaleX + amount);
                        if (_wallUVScaleXBox != null)
                            _wallUVScaleXBox.Text = _selectedSector.WallUVScaleX.ToString("F2");
                    }
                    else if (axis == "Y")
                    {
                        _selectedSector.WallUVScaleY = Math.Max(0.1f, _selectedSector.WallUVScaleY + amount);
                        if (_wallUVScaleYBox != null)
                            _wallUVScaleYBox.Text = _selectedSector.WallUVScaleY.ToString("F2");
                    }

                    break;
            }
        }

        // UV Rotation Controls
        private void OnUVRotationChanged(string textureType, string value)
        {
            if (_selectedSector == null || !float.TryParse(value, out float rotationValue)) return;

            switch (textureType.ToLower())
            {
                case "floor":
                    _selectedSector.FloorUVRotation = rotationValue;
                    break;
                case "ceiling":
                    _selectedSector.CeilingUVRotation = rotationValue;
                    break;
                case "wall":
                    _selectedSector.WallUVRotation = rotationValue;
                    break;
            }
        }

        private void AdjustUVRotation(string textureType, float amount)
        {
            if (_selectedSector == null) return;

            switch (textureType.ToLower())
            {
                case "floor":
                    _selectedSector.FloorUVRotation = (_selectedSector.FloorUVRotation + amount) % 360f;
                    if (_floorUVRotationBox != null)
                        _floorUVRotationBox.Text = _selectedSector.FloorUVRotation.ToString("F1");
                    break;
                case "ceiling":
                    _selectedSector.CeilingUVRotation = (_selectedSector.CeilingUVRotation + amount) % 360f;
                    if (_ceilingUVRotationBox != null)
                        _ceilingUVRotationBox.Text = _selectedSector.CeilingUVRotation.ToString("F1");
                    break;
                case "wall":
                    _selectedSector.WallUVRotation = (_selectedSector.WallUVRotation + amount) % 360f;
                    if (_wallUVRotationBox != null)
                        _wallUVRotationBox.Text = _selectedSector.WallUVRotation.ToString("F1");
                    break;
            }
        }

        // Shading Controls
        private void OnShadingChanged(string textureType, string value)
        {
            if (_selectedSector == null || !float.TryParse(value, out float shadingValue)) return;
            shadingValue = Math.Max(0f, Math.Min(2f, shadingValue)); // Clamp between 0 and 2

            switch (textureType.ToLower())
            {
                case "floor":
                    _selectedSector.FloorShading = shadingValue;
                    break;
                case "ceiling":
                    _selectedSector.CeilingShading = shadingValue;
                    break;
                case "wall":
                    _selectedSector.WallShading = shadingValue;
                    break;
            }
        }

        private void AdjustShading(string textureType, float amount)
        {
            if (_selectedSector == null) return;

            switch (textureType.ToLower())
            {
                case "floor":
                    _selectedSector.FloorShading = Math.Max(0f, Math.Min(2f, _selectedSector.FloorShading + amount));
                    if (_floorShadingBox != null) _floorShadingBox.Text = _selectedSector.FloorShading.ToString("F2");
                    break;
                case "ceiling":
                    _selectedSector.CeilingShading =
                        Math.Max(0f, Math.Min(2f, _selectedSector.CeilingShading + amount));
                    if (_ceilingShadingBox != null)
                        _ceilingShadingBox.Text = _selectedSector.CeilingShading.ToString("F2");
                    break;
                case "wall":
                    _selectedSector.WallShading = Math.Max(0f, Math.Min(2f, _selectedSector.WallShading + amount));
                    if (_wallShadingBox != null) _wallShadingBox.Text = _selectedSector.WallShading.ToString("F2");
                    break;
            }
        }

        private void Handle3DSelection(MouseState mouseState)
        {
            var viewportBounds = GetViewportBounds();
            bool mouseInViewport = viewportBounds.Contains(mouseState.Position);

            if (!mouseInViewport)
                return;

            // Update hovered sprite detection
            _hoveredSprite3D = Get3DSpriteUnderCursor(mouseState);

            // Handle left mouse button for sprite selection and gizmo interaction
            if (mouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                var ray = ScreenPointToRay(mouseState.Position);

                // If sprite is selected, check for gizmo interaction first
                if (_selectedSprite3D != null)
                {
                    var gizmoPosition = Get3DSpriteWorldPosition(_selectedSprite3D);
                    var hitGizmo = GetGizmoAxisUnderMouse(gizmoPosition, ray);

                    if (hitGizmo != "none")
                    {
                        // Start gizmo dragging
                        _hoveredGizmo = hitGizmo;
                        _isDraggingGizmo = true;
                        _gizmoStartPosition = gizmoPosition;
                        _dragStartMouse = new Vector2(mouseState.Position.X, mouseState.Position.Y);
                    }
                    else
                    {
                        // Check if clicking on another sprite
                        if (_hoveredSprite3D != null)
                        {
                            _selectedSprite3D = _hoveredSprite3D;
                            _selectedSprite = _hoveredSprite3D;
                            _gizmoStartPosition = Get3DSpriteWorldPosition(_hoveredSprite3D);
                        }
                        else
                        {
                            // Deselect sprite but don't close editors in 3D mode
                            _selectedSprite3D = null;
                            _selectedSprite = null;
                            // Keep editors open in 3D mode
                            // _spriteEditorVisible = false; // Don't close in 3D
                        }
                    }
                }
                else if (_hoveredSprite3D != null)
                {
                    // Select sprite and show gizmos
                    _selectedSprite3D = _hoveredSprite3D;
                    _selectedSprite = _hoveredSprite3D;
                    _gizmoStartPosition = Get3DSpriteWorldPosition(_hoveredSprite3D);
                }
                else
                {
                    // In slope mode, check for vertex selection
                    if (_currentEditMode == EditMode.SlopeEdit)
                    {
                        // Require a selected sector before allowing slope editing
                        if (_selectedSector != null)
                        {
                            var vertexRay = ScreenPointToRay(mouseState.Position);
                            var selectedVertex = Get3DVertexUnderCursor(vertexRay);
                            if (selectedVertex.HasValue && _selectedSector.Vertices.Contains(selectedVertex.Value))
                            {
                                _selectedVertex = selectedVertex.Value;
                                var vertexIndex = _selectedSector.Vertices.IndexOf(selectedVertex.Value);
                                _vertexDragStartHeight =
                                    GetVertexHeight(_selectedSector, vertexIndex, _isEditingFloorSlope);
                            }
                            else
                            {
                                _selectedVertex = null;
                            }
                        }
                    }
                    else
                    {
                        // Check for sector selection using proper ray casting
                        var clickedSector = Get3DSectorUnderCursor(ray);
                        if (clickedSector != null)
                        {
                            _selectedSector = clickedSector;
                            // Don't close the sector properties window in 3D mode
                            // _sectorPropertiesWindow.Visible = false;
                        }
                    }
                }
            }

            // Handle gizmo dragging
            if (_isDraggingGizmo && mouseState.LeftButton == ButtonState.Pressed && _selectedSprite3D != null)
            {
                var currentMousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);
                var mouseDelta = currentMousePos - _dragStartMouse;
                var dragSensitivity = 1.0f; // Increased sensitivity for better responsiveness

                // Apply movement based on selected axis
                var currentPos = _selectedSprite3D.Position;
                var currentHeight = _selectedSprite3D.Height;
                switch (_hoveredGizmo)
                {
                    case "X":
                        // X-axis: left/right movement with mouse X
                        _selectedSprite3D.Position =
                            new Vector2(currentPos.X + mouseDelta.X * dragSensitivity, currentPos.Y);
                        break;
                    case "Y":
                        // Y-axis: up/down movement - use Height property for vertical positioning
                        var newHeight = currentHeight + -mouseDelta.Y * dragSensitivity * 5f;
                        _selectedSprite3D.Height = newHeight; // Allow negative heights for below-floor positioning
                        Console.WriteLine(
                            $"Y-axis drag: mouseDelta.Y={mouseDelta.Y}, oldHeight={currentHeight}, newHeight={_selectedSprite3D.Height}");
                        break;
                    case "Z":
                        // Z-axis: forward/backward movement - use Position.Y for depth (Build engine convention)
                        var newZ = currentPos.Y + -mouseDelta.X * dragSensitivity;
                        _selectedSprite3D.Position = new Vector2(currentPos.X, newZ);
                        Console.WriteLine(
                            $"Z-axis drag: mouseDelta.X={mouseDelta.X}, oldZ={currentPos.Y}, newZ={newZ}");
                        break;
                }

                // Update drag start for continuous movement
                _dragStartMouse = currentMousePos;
            }

            // Stop gizmo dragging on mouse release
            if (_isDraggingGizmo && mouseState.LeftButton == ButtonState.Released)
            {
                _isDraggingGizmo = false;
                _hoveredGizmo = "none";
            }

            // Handle vertex height dragging in 3D slope mode
            if (_currentEditMode == EditMode.SlopeEdit && _selectedVertex.HasValue &&
                _selectedSector != null && mouseState.LeftButton == ButtonState.Pressed)
            {
                var currentMousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);

                if (!_isDraggingVertex)
                {
                    _isDraggingVertex = true;
                    _dragStartMouse = currentMousePos;
                }

                var mouseDelta = currentMousePos - _dragStartMouse;

                // Use the selected sector for vertex editing
                if (_selectedSector.Vertices.Contains(_selectedVertex.Value))
                {
                    var vertexIndex = _selectedSector.Vertices.IndexOf(_selectedVertex.Value);
                    var currentHeight = GetVertexHeight(_selectedSector, vertexIndex, _isEditingFloorSlope);

                    // Vertical mouse movement adjusts height
                    var newHeight = currentHeight + -mouseDelta.Y * 0.5f; // Sensitivity adjustment
                    SetVertexHeight(_selectedSector, vertexIndex, newHeight, _isEditingFloorSlope);

                    // Mark sector as having slopes
                    _selectedSector.HasSlopes = true;
                }

                _dragStartMouse = currentMousePos;
            }

            // Stop vertex dragging on mouse release
            if (_isDraggingVertex && mouseState.LeftButton == ButtonState.Released)
            {
                _isDraggingVertex = false;
            }

            // Handle sprite mouse tracking mode (G key toggle)
            if (_spriteMouseTrackMode && _selectedSprite3D != null)
            {
                UpdateSpriteMouseTracking(mouseState);
            }

            // Stop dragging when mouse is released
            if (mouseState.LeftButton == ButtonState.Released &&
                _previousMouseState.LeftButton == ButtonState.Pressed)
            {
                _isDragging3DSprite = false;
                
                // Ensure sprite position is finalized when dragging ends
                if (_selectedSprite != null)
                {
                    // The position should already be set during dragging, but ensure it's maintained
                    // This helps prevent any position reset issues
                }
            }
        }

        private void UpdateSpriteMouseTracking(MouseState mouseState)
        {
            // Cast ray from mouse to find intersection with geometry
            var ray = ScreenPointToRay(mouseState.Position);
            
            
            float closestDistance = float.MaxValue;
            Vector3 intersectionPoint = Vector3.Zero;
            Sector intersectionSector = null;
            Wall intersectionWall = null;
            bool isWallIntersection = false;
            
            // Check all sectors for floor/ceiling intersections
            foreach (var sector in _sectors)
            {
                // Check floor intersection
                var floorHeight = sector.FloorHeight + sector.AnimationHeightOffset;
                var floorPlanePoint = new Vector3(0, floorHeight, 0);
                var floorPlaneNormal = Vector3.Up;
                
                if (RayIntersectsPlane(ray, floorPlanePoint, floorPlaneNormal, out Vector3 floorPoint, out float floorDistance))
                {
                    Vector2 floorPoint2D = new Vector2(floorPoint.X, -floorPoint.Z);
                    bool inSector = IsPointInSector(floorPoint2D, sector);
                    
                    if (inSector && floorDistance < closestDistance)
                    {
                        closestDistance = floorDistance;
                        intersectionPoint = floorPoint;
                        intersectionSector = sector;
                        intersectionWall = null;
                        isWallIntersection = false;
                    }
                }
                
                // Check ceiling intersection
                var ceilingHeight = sector.CeilingHeight + sector.AnimationHeightOffset;
                if (RayIntersectsPlane(ray, new Vector3(0, ceilingHeight, 0), Vector3.Down, out Vector3 ceilingPoint, out float ceilingDistance))
                {
                    if (IsPointInSector(new Vector2(ceilingPoint.X, -ceilingPoint.Z), sector) && ceilingDistance < closestDistance)
                    {
                        closestDistance = ceilingDistance;
                        intersectionPoint = ceilingPoint;
                        intersectionSector = sector;
                        intersectionWall = null;
                        isWallIntersection = false;
                    }
                }
                
                // Check wall intersections
                foreach (var wall in sector.Walls)
                {
                    if (RayIntersectsWall(ray, wall, sector, out Vector3 wallPoint, out float wallDistance, out Vector3 wallNormal))
                    {
                        if (wallDistance < closestDistance)
                        {
                            closestDistance = wallDistance;
                            intersectionPoint = wallPoint;
                            intersectionSector = sector;
                            intersectionWall = wall;
                            isWallIntersection = true;
                        }
                    }
                }
            }
            
            // If we found an intersection, update the sprite
            if (intersectionSector != null && closestDistance < float.MaxValue)
            {
                if (isWallIntersection && intersectionWall != null)
                {
                    // Wall intersection - sprite should be positioned ON the wall surface
                    Vector2 spritePosition = new Vector2(intersectionPoint.X, -intersectionPoint.Z);
                    
                    // Calculate wall angle using your formula - fix for diagonal walls
                    float x1 = intersectionWall.Start.X;
                    float y1 = intersectionWall.Start.Y;
                    float x2 = intersectionWall.End.X;
                    float y2 = intersectionWall.End.Y;
                    
                    float dx = x2 - x1;
                    float dy = y2 - y1;
                    float angleRadians = MathF.Atan2(dy, dx);  // radians
                    float angleDegrees = angleRadians * (180f / MathF.PI);
                    
                    
                    // Normalize angle to 0-360 range - sprite should be parallel to wall
                    if (angleDegrees < 0) angleDegrees += 360f;
                    
                    // Sprite angle should match wall direction vector exactly (Build engine style)
                    float wallFacingAngle = angleDegrees; // Direct wall angle - let renderer handle quad orientation
                    
                    
                    // Calculate height from floor to intersection point
                    float spriteHeightFromFloor = intersectionPoint.Y - intersectionSector.FloorHeight;
                    
                    // Set sprite properties for wall alignment
                    _selectedSprite3D.Position = spritePosition;
                    _selectedSprite3D.Alignment = SpriteAlignment.Wall;
                    _selectedSprite3D.Angle = wallFacingAngle; // Face perpendicular to wall for proper alignment
                    _selectedSprite3D.Height = Math.Max(16f, spriteHeightFromFloor);
                    
                    
                    // Store position for persistence when exiting G mode
                    _lastSpriteTrackPosition = spritePosition;
                    _lastSpriteTrackHeight = Math.Max(16f, spriteHeightFromFloor);
                    _lastSpriteTrackAngle = wallFacingAngle;
                    _lastSpriteTrackAlignment = SpriteAlignment.Wall;
                }
                else
                {
                    // Floor/ceiling intersection - use the cursor snap type to determine which surface was hit
                    Vector2 spritePosition = new Vector2(intersectionPoint.X, -intersectionPoint.Z);
                    
                    // Determine if intersection is closer to floor or ceiling using direct Y comparison
                    float floorHeight = intersectionSector.FloorHeight;
                    float ceilingHeight = intersectionSector.CeilingHeight;
                    float midpoint = (floorHeight + ceilingHeight) / 2f;
                    bool isFloorSnap = intersectionPoint.Y < midpoint;
                    
                    
                    // Floor/ceiling sprites don't need wall angle alignment
                    _selectedSprite3D.Position = spritePosition;
                    _selectedSprite3D.Angle = 0f; // Floor/ceiling sprites use default angle
                    
                    if (isFloorSnap)
                    {
                        // Floor snapping with tiny vertical offset
                        float floorOffset = 0.05f; // Minimal offset from floor surface
                        _selectedSprite3D.Alignment = SpriteAlignment.Floor;
                        _selectedSprite3D.Height = floorOffset;
                        
                        
                        // Store position for persistence when exiting G mode
                        _lastSpriteTrackPosition = spritePosition;
                        _lastSpriteTrackHeight = floorOffset;
                        _lastSpriteTrackAngle = 0f;
                        _lastSpriteTrackAlignment = SpriteAlignment.Floor;
                    }
                    else
                    {
                        // Ceiling snapping - use Floor alignment with tiny offset from ceiling
                        float ceilingOffset = 0.05f; // Minimal offset from ceiling surface
                        float spriteHeightFromFloor = intersectionSector.CeilingHeight - intersectionSector.FloorHeight - ceilingOffset;
                        
                        _selectedSprite3D.Alignment = SpriteAlignment.Floor;
                        _selectedSprite3D.Height = spriteHeightFromFloor;
                        
                        
                        // Store position for persistence when exiting G mode
                        _lastSpriteTrackPosition = spritePosition;
                        _lastSpriteTrackHeight = spriteHeightFromFloor;
                        _lastSpriteTrackAngle = 0f;
                        _lastSpriteTrackAlignment = SpriteAlignment.Floor;
                    }
                }
            }
        }

        private Sprite Get3DSpriteUnderCursor(MouseState mouseState)
        {
            // Convert mouse position to 3D ray
            var ray = ScreenPointToRay(mouseState.Position);

            float closestDistance = float.MaxValue;
            Sprite closestSprite = null;

            // Check all sprites in all sectors
            foreach (var sector in _sectors)
            {
                foreach (var sprite in sector.Sprites)
                {
                    // Get sprite's 3D world position
                    var spriteWorldPos = Get3DSpriteWorldPosition(sprite);

                    // Calculate distance from ray to sprite center
                    var toSprite = spriteWorldPos - ray.Position;
                    var projectionLength = Vector3.Dot(toSprite, ray.Direction);

                    // Skip if sprite is behind ray origin
                    if (projectionLength < 0) continue;

                    var projectedPoint = ray.Position + ray.Direction * projectionLength;
                    var distance = Vector3.Distance(spriteWorldPos, projectedPoint);

                    // Much more generous radius for floating sprites - use both height and a base radius
                    var baseRadius = 48f; // Large base selection radius
                    var heightRadius = Math.Max(sprite.Height * 0.8f, 32f); // Height-based radius
                    var spriteRadius = Math.Max(baseRadius, heightRadius);

                    if (distance <= spriteRadius && projectionLength < closestDistance)
                    {
                        closestDistance = projectionLength;
                        closestSprite = sprite;
                    }
                }
            }

            return closestSprite;
        }

        private Vector2? Get3DVertexUnderCursor(Ray ray)
        {
            // Only check vertices in the selected sector
            if (_selectedSector == null) return null;

            float closestDistance = float.MaxValue;
            Vector2? closestVertex = null;

            foreach (var vertex in _selectedSector.Vertices)
            {
                var vertexIndex = _selectedSector.Vertices.IndexOf(vertex);

                // Get vertex 3D position using current floor/ceiling heights being edited
                float vertexHeight = GetVertexHeight(_selectedSector, vertexIndex, _isEditingFloorSlope);
                var vertex3D = new Vector3(vertex.X, vertexHeight, -vertex.Y);

                // Calculate distance from ray to vertex
                var toVertex = vertex3D - ray.Position;
                var projectionLength = Vector3.Dot(toVertex, ray.Direction);

                // Skip if vertex is behind ray origin
                if (projectionLength < 0) continue;

                var projectedPoint = ray.Position + ray.Direction * projectionLength;
                var distance = Vector3.Distance(vertex3D, projectedPoint);

                // Generous selection radius for vertices in 3D
                var selectionRadius = 20f;

                if (distance <= selectionRadius && projectionLength < closestDistance)
                {
                    closestDistance = projectionLength;
                    closestVertex = vertex;
                }
            }

            return closestVertex;
        }

        private Vector3 Get3DSpriteWorldPosition(Sprite sprite)
        {
            // Find the sector containing this sprite
            var sector = _sectors.FirstOrDefault(s => s.Sprites.Contains(sprite));
            if (sector == null) return Vector3.Zero;

            // Convert 2D sprite position to 3D world coordinates based on alignment
            float spriteY;
            switch (sprite.Alignment)
            {
                case SpriteAlignment.Floor:
                    // Sprite sits on the floor - use Height as offset from floor
                    spriteY = sector.FloorHeight + sprite.Height;
                    break;

                case SpriteAlignment.Wall:
                    // Wall sprite - use Height as offset from floor (not mid-height)
                    spriteY = sector.FloorHeight + sprite.Height;
                    break;

                case SpriteAlignment.Face:
                default:
                    // Billboard sprite - use Height as offset from floor
                    spriteY = sector.FloorHeight + sprite.Height;
                    break;
            }

            return new Vector3(sprite.Position.X, spriteY, -sprite.Position.Y);
        }

        private void CheckAndApplyAutoAlignment(Sprite sprite)
        {
            if (sprite == null) return;

            Console.WriteLine($"Auto-alignment check: snapType={_cursor3DSnapType}, cursorPos={_cursor3DPosition}");

            // Only apply if cursor position has changed significantly (to avoid constant updates)
            var positionDelta = Vector3.Distance(_cursor3DPosition, _lastAutoAlignPosition);
            if (positionDelta < 1f) return; // Only update if cursor moved at least 1 unit

            // Use the current 3D cursor position and snap type to position the sprite
            if (_cursor3DSnapType != "none")
            {
                _lastAutoAlignPosition = _cursor3DPosition;
                // Position sprite at cursor location
                sprite.Position = new Vector2(_cursor3DPosition.X, -_cursor3DPosition.Z);

                // Set alignment and height based on cursor snap type
                switch (_cursor3DSnapType)
                {
                    case "floor":
                        sprite.Alignment = SpriteAlignment.Floor;
                        sprite.Height = _cursor3DPosition.Y - (_cursor3DSector?.FloorHeight ?? 0f);

                        // For floor sprites, they should lay flat (like a carpet)
                        sprite.Angle = 0f; // Parallel to floor surface

                        Console.WriteLine($"Auto-aligned sprite to floor parallel to surface");
                        break;

                    case "wall":
                        sprite.Alignment = SpriteAlignment.Wall;
                        sprite.Height = _cursor3DPosition.Y - (_cursor3DSector?.FloorHeight ?? 0f);

                        // Offset sprite slightly inward from wall surface to prevent clipping
                        if (_cursor3DNormal != Vector3.Zero)
                        {
                            const float wallOffset = 2f; // Small offset inward from wall
                            Vector3 offsetPosition = _cursor3DPosition + _cursor3DNormal * wallOffset;
                            sprite.Position = new Vector2(offsetPosition.X, -offsetPosition.Z);
                        }

                        // Set sprite angle to be parallel to the wall (like a picture frame)
                        if (_cursor3DWall != null)
                        {
                            var wallVector = _cursor3DWall.End - _cursor3DWall.Start;
                            var wallAngle = Math.Atan2(wallVector.Y, wallVector.X) * (180.0 / Math.PI);
                            sprite.Angle = (float)wallAngle; // Parallel to wall, not perpendicular
                        }

                        Console.WriteLine($"Auto-aligned sprite to wall parallel with angle {sprite.Angle}, offset inward");
                        break;

                    case "ceiling":
                        sprite.Alignment = SpriteAlignment.Face;
                        sprite.Height = _cursor3DPosition.Y - (_cursor3DSector?.FloorHeight ?? 0f);

                        // For ceiling sprites, they should lay flat on ceiling (parallel to surface)
                        sprite.Angle = 0f; // Parallel to ceiling surface

                        Console.WriteLine($"Auto-aligned sprite to ceiling parallel to surface");
                        break;
                }
            }
        }

        // Slope system helper methods
        private float GetVertexHeight(Sector sector, int vertexIndex, bool isFloor)
        {
            // Comprehensive bounds checking and validation
            if (sector == null)
            {
                Console.WriteLine($"WARNING: GetVertexHeight called with null sector");
                return 0f;
            }

            if (vertexIndex < 0 || vertexIndex >= sector.Vertices.Count)
            {
                Console.WriteLine($"WARNING: GetVertexHeight called with invalid vertex index {vertexIndex} for sector {sector.Id} (vertex count: {sector.Vertices.Count})");
                return isFloor ? sector.FloorHeight : sector.CeilingHeight;
            }

            var vertexHeight = sector.VertexHeights.FirstOrDefault(vh => vh.VertexIndex == vertexIndex);
            if (vertexHeight != null)
            {
                float height = isFloor ? vertexHeight.FloorHeight : vertexHeight.CeilingHeight;
                
                // Sanity check for reasonable height values
                if (float.IsNaN(height) || float.IsInfinity(height))
                {
                    Console.WriteLine($"WARNING: Invalid height value {height} for vertex {vertexIndex} in sector {sector.Id}");
                    return isFloor ? sector.FloorHeight : sector.CeilingHeight;
                }
                
                return height;
            }

            return isFloor ? sector.FloorHeight : sector.CeilingHeight;
        }

        private void SetVertexHeight(Sector sector, int vertexIndex, float height, bool isFloor)
        {
            // Comprehensive bounds checking and validation
            if (sector == null)
            {
                Console.WriteLine($"ERROR: SetVertexHeight called with null sector");
                return;
            }

            if (vertexIndex < 0 || vertexIndex >= sector.Vertices.Count)
            {
                Console.WriteLine($"ERROR: SetVertexHeight called with invalid vertex index {vertexIndex} for sector {sector.Id} (vertex count: {sector.Vertices.Count})");
                return;
            }

            if (float.IsNaN(height) || float.IsInfinity(height))
            {
                Console.WriteLine($"ERROR: Attempted to set invalid height value {height} for vertex {vertexIndex} in sector {sector.Id}");
                return;
            }

            // Reasonable height bounds checking (Build engine style limits)
            const float MIN_HEIGHT = -8192f;
            const float MAX_HEIGHT = 8192f;
            if (height < MIN_HEIGHT || height > MAX_HEIGHT)
            {
                Console.WriteLine($"WARNING: Height value {height} is outside reasonable bounds [{MIN_HEIGHT}, {MAX_HEIGHT}] for vertex {vertexIndex} in sector {sector.Id}");
                height = Math.Max(MIN_HEIGHT, Math.Min(MAX_HEIGHT, height));
            }

            // CRITICAL FIX: Initialize ALL vertex heights if none exist to prevent collision system auto-generation
            if (sector.VertexHeights.Count == 0)
            {
                Console.WriteLine($"Initializing vertex heights for all {sector.Vertices.Count} vertices in sector {sector.Id}");
                for (int i = 0; i < sector.Vertices.Count; i++)
                {
                    var vh = new VertexHeight
                    {
                        VertexIndex = i,
                        FloorHeight = sector.FloorHeight,
                        CeilingHeight = sector.CeilingHeight
                    };
                    sector.VertexHeights.Add(vh);
                }
            }

            var vertexHeight = sector.VertexHeights.FirstOrDefault(vh => vh.VertexIndex == vertexIndex);
            if (vertexHeight == null)
            {
                vertexHeight = new VertexHeight { VertexIndex = vertexIndex };
                sector.VertexHeights.Add(vertexHeight);
            }

            // Store previous value for change detection
            float previousHeight = isFloor ? vertexHeight.FloorHeight : vertexHeight.CeilingHeight;

            if (isFloor)
                vertexHeight.FloorHeight = height;
            else
                vertexHeight.CeilingHeight = height;

            sector.HasSlopes = true;

            // Invalidate slope plane cache when vertex heights change
            if (Math.Abs(previousHeight - height) > 0.001f)
            {
                InvalidateSlopePlaneCache(sector.Id, isFloor);
                
                // Also invalidate the opposite surface if height change is significant
                // (floor changes can affect ceiling slope calculations in complex scenarios)
                if (Math.Abs(previousHeight - height) > 10f)
                {
                    InvalidateSlopePlaneCache(sector.Id, !isFloor);
                }
            }
        }
        
        private void InvalidateSlopePlaneCache(int sectorId, bool isFloor)
        {
            var cacheKey = (sectorId, isFloor);
            if (_slopePlaneCache.ContainsKey(cacheKey))
            {
                _slopePlaneCache.Remove(cacheKey);
                // Console.WriteLine($"DEBUG: Invalidated slope plane cache for sector {sectorId}, {(isFloor ? "floor" : "ceiling")}");
            }
        }
        
        // Method to clear all slope plane cache (useful for major changes)
        private void ClearSlopePlaneCache()
        {
            int cacheCount = _slopePlaneCache.Count;
            _slopePlaneCache.Clear();
            // Console.WriteLine($"DEBUG: Cleared {cacheCount} entries from slope plane cache");
        }
        
        // Enhanced validation method for entire sectors
        private bool ValidateSectorSlopeData(Sector sector)
        {
            if (sector == null) return false;
            
            bool isValid = true;
            
            // Check for vertex height consistency
            foreach (var vh in sector.VertexHeights)
            {
                if (vh.VertexIndex < 0 || vh.VertexIndex >= sector.Vertices.Count)
                {
                    Console.WriteLine($"ERROR: Invalid vertex index {vh.VertexIndex} in sector {sector.Id} vertex heights");
                    isValid = false;
                }
                
                if (float.IsNaN(vh.FloorHeight) || float.IsInfinity(vh.FloorHeight) ||
                    float.IsNaN(vh.CeilingHeight) || float.IsInfinity(vh.CeilingHeight))
                {
                    Console.WriteLine($"ERROR: Invalid height values in sector {sector.Id} vertex {vh.VertexIndex}");
                    isValid = false;
                }
                
                // Floor should generally be below ceiling
                if (vh.FloorHeight > vh.CeilingHeight + 1f) // Allow small tolerance
                {
                    Console.WriteLine($"WARNING: Floor height {vh.FloorHeight} above ceiling height {vh.CeilingHeight} for vertex {vh.VertexIndex} in sector {sector.Id}");
                }
            }
            
            // Validate slope planes if they exist
            if (sector.FloorPlane != null)
            {
                if (!ValidateSlopePlane(sector.FloorPlane, sector.Id, true))
                    isValid = false;
            }
            
            if (sector.CeilingPlane != null)
            {
                if (!ValidateSlopePlane(sector.CeilingPlane, sector.Id, false))
                    isValid = false;
            }
            
            return isValid;
        }
        
        private bool ValidateSlopePlane(SlopePlane plane, int sectorId, bool isFloor)
        {
            if (plane == null) return false;
            
            bool isValid = true;
            
            if (float.IsNaN(plane.BaseZ) || float.IsInfinity(plane.BaseZ) ||
                float.IsNaN(plane.DeltaX) || float.IsInfinity(plane.DeltaX) ||
                float.IsNaN(plane.DeltaY) || float.IsInfinity(plane.DeltaY))
            {
                // Console.WriteLine($"ERROR: Invalid slope plane values for sector {sectorId} {(isFloor ? "floor" : "ceiling")}");
                isValid = false;
            }
            
            // Check for extremely steep slopes that might cause issues
            float maxSlope = 10f; // Maximum slope of 10 units per world unit
            if (Math.Abs(plane.DeltaX) > maxSlope || Math.Abs(plane.DeltaY) > maxSlope)
            {
                // Console.WriteLine($"WARNING: Very steep slope detected for sector {sectorId} {(isFloor ? "floor" : "ceiling")} - dx:{plane.DeltaX}, dy:{plane.DeltaY}");
            }
            
            return isValid;
        }

        private void HandleSlopeEdit(MouseState mouseState)
        {
            if (_currentEditMode != EditMode.SlopeEdit)
                return;

            // Don't handle input if mouse is over UI
            if (_desktop.IsMouseOverGUI)
                return;

            // Require a selected sector before allowing slope editingco
            if (_selectedSector == null)
                return;

            // Find hovered vertex, but only within the selected sector
            var hoveredVertex = GetVertexAt(_mouseWorldPosition, 8f);

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                if (hoveredVertex.HasValue)
                {
                    // Check if this vertex belongs to the selected sector
                    if (_selectedSector.Vertices.Contains(hoveredVertex.Value))
                    {
                        _selectedVertex = hoveredVertex;
                        _isDraggingVertex = true;

                        var vertexIndex = _selectedSector.Vertices.IndexOf(hoveredVertex.Value);
                        _vertexDragStartHeight = GetVertexHeight(_selectedSector, vertexIndex, _isEditingFloorSlope);
                    }
                }
            }

            if (_isDraggingVertex && mouseState.LeftButton == ButtonState.Pressed && _selectedVertex.HasValue)
            {
                var sector = GetSectorContainingVertex(_selectedVertex.Value);
                if (sector != null)
                {
                    var vertexIndex = sector.Vertices.IndexOf(_selectedVertex.Value);
                    var mouseDelta = mouseState.Position.ToVector2() - _previousMouseState.Position.ToVector2();
                    var newHeight = _vertexDragStartHeight + -mouseDelta.Y * 0.5f;

                    SetVertexHeight(sector, vertexIndex, newHeight, _isEditingFloorSlope);
                }
            }

            if (mouseState.LeftButton == ButtonState.Released)
            {
                _isDraggingVertex = false;
            }
        }

        private Sector GetSectorContainingVertex(Vector2 vertex)
        {
            foreach (var sector in _sectors)
            {
                if (sector.Vertices.Contains(vertex))
                    return sector;
            }

            return null;
        }

        private float DistancePointToWall(Vector2 point, Wall wall)
        {
            var wallStart = wall.Start;
            var wallEnd = wall.End;
            var wallLength = Vector2.Distance(wallStart, wallEnd);

            if (wallLength == 0) return Vector2.Distance(point, wallStart);

            var t = Math.Max(0,
                Math.Min(1, Vector2.Dot(point - wallStart, wallEnd - wallStart) / (wallLength * wallLength)));
            var projection = wallStart + t * (wallEnd - wallStart);
            return Vector2.Distance(point, projection);
        }

        private bool IsPointInWindow(Window window, Point mousePos)
        {
            if (!window.Visible) return false;

            // Get the actual bounds of the window
            var bounds = window.Bounds;
            return bounds.Contains(mousePos);
        }

        private void ShowTextureDropdown(string textureType, Button sourceButton)
        {
            _currentTextureType = textureType;

            // Create dropdown window
            _textureDropdownWindow = new Window
            {
                Title = "Select Texture",
                Left = 300,
                Top = 200,
                Width = 200,
                Height = 300,
                Visible = true
            };

            // Create scrollable pane for texture list
            var scrollPane = new ScrollViewer
            {
                ShowHorizontalScrollBar = false,
                ShowVerticalScrollBar = true
            };

            var dropdownGrid = new Grid
            {
                RowSpacing = 2
            };

            // Only show geometry textures loaded from Content.mgcb (use display names)
            var availableTextures = new List<string>();
            foreach (var kvp in _geometryTextures)
            {
                // Use display name (filename without path/extension)
                string displayName = kvp.Key.Contains("/") ? Path.GetFileNameWithoutExtension(kvp.Key) : kvp.Key;
                availableTextures.Add(displayName);
            }

            // Sort alphabetically
            availableTextures.Sort();

            for (int i = 0; i < availableTextures.Count; i++)
            {
                dropdownGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            }

            dropdownGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

            int row = 0;
            foreach (var textureName in availableTextures)
            {
                var textureButton = new Button
                {
                    Content = new Label { Text = textureName },
                    Height = 25
                };

                string capturedTextureName = textureName; // Capture for closure
                textureButton.Click += (s, e) =>
                {
                    SelectTexture(capturedTextureName);
                    _textureDropdownWindow.Visible = false;
                    _desktop.Widgets.Remove(_textureDropdownWindow);
                };

                dropdownGrid.Widgets.Add(textureButton);
                Grid.SetRow(textureButton, row);
                row++;
            }

            scrollPane.Content = dropdownGrid;
            _textureDropdownWindow.Content = scrollPane;
            _desktop.Widgets.Add(_textureDropdownWindow);
        }

        private void SelectTexture(string textureName)
        {
            if (_selectedSector == null) return;

            switch (_currentTextureType)
            {
                case "floor":
                    _selectedSector.FloorTexture = textureName;
                    if (_floorTextureButtonTex != null)
                        ((Label)_floorTextureButtonTex.Content).Text = textureName;
                    break;
                case "ceiling":
                    _selectedSector.CeilingTexture = textureName;
                    if (_ceilingTextureButtonTex != null)
                        ((Label)_ceilingTextureButtonTex.Content).Text = textureName;
                    break;
                case "wall":
                    _selectedSector.WallTexture = textureName;
                    if (_wallTextureButtonTex != null)
                        ((Label)_wallTextureButtonTex.Content).Text = textureName;
                    break;
            }
        }

        private Texture2D GetTexture(string textureName)
        {
            // First try direct lookup in geometry textures
            if (_geometryTextures.TryGetValue(textureName, out Texture2D geometryTexture))
                return geometryTexture;

            // Try sprite textures
            if (_spriteTextures.TryGetValue(textureName, out Texture2D spriteTexture))
                return spriteTexture;

            // Try legacy loaded textures
            if (_loadedTextures.TryGetValue(textureName, out Texture2D loadedTexture))
                return loadedTexture;

            // Try to find by display name (search all geometry textures for matching filename)
            foreach (var kvp in _geometryTextures)
            {
                string displayName = kvp.Key.Contains("/") ? Path.GetFileNameWithoutExtension(kvp.Key) : kvp.Key;
                if (displayName.Equals(textureName, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            // Return default texture or null if not found
            return _defaultTexture;
        }

        private Color GetTextureColor(string textureName)
        {
            // Get the actual texture
            var texture = GetTexture(textureName);
            
            if (texture == null || texture == _defaultTexture)
            {
                // Fallback to legacy color system if available
                if (_textureColors.ContainsKey(textureName))
                    return _textureColors[textureName];
                
                // Default colors based on texture name
                if (textureName.ToLower().Contains("floor"))
                    return Color.Gray;
                if (textureName.ToLower().Contains("ceiling"))
                    return Color.LightGray;
                if (textureName.ToLower().Contains("wall"))
                    return Color.White;
                
                return Color.White;
            }

            // Sample the texture to get a representative color
            try
            {
                // Get texture data (this is expensive, so we might want to cache this)
                var data = new Color[texture.Width * texture.Height];
                texture.GetData(data);
                
                // Sample a few pixels and average them
                int sampleCount = Math.Min(100, data.Length); // Sample up to 100 pixels
                int step = Math.Max(1, data.Length / sampleCount);
                
                long r = 0, g = 0, b = 0;
                int actualSamples = 0;
                
                for (int i = 0; i < data.Length; i += step)
                {
                    var pixel = data[i];
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    actualSamples++;
                }
                
                if (actualSamples > 0)
                {
                    return new Color((int)(r / actualSamples), (int)(g / actualSamples), (int)(b / actualSamples));
                }
            }
            catch (Exception)
            {
                // If texture sampling fails, fall back to white
            }
            
            return Color.White;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Load textures automatically from Content.mgcb
            LoadTexturesFromContent();
            
            // Load sprite textures (fallback procedural textures)
            LoadSpriteTextures();

            // Load wall/floor/ceiling textures (fallback procedural textures)
            LoadWallTextures();

            // Initialize 3D effect
            _basicEffect = new BasicEffect(GraphicsDevice);
            _basicEffect.TextureEnabled = true;
            _basicEffect.VertexColorEnabled = false;
            _basicEffect.LightingEnabled = false;
        }

        private void LoadTexturesFromContent()
        {
            try
            {
                string currentDir = Directory.GetCurrentDirectory();
                string contentPath;
                
                // When running from bin/Debug/net8.0, we need to go back to the project source
                if (currentDir.Contains("bin") && currentDir.Contains("Debug"))
                {
                    // Go back from bin/Debug/net8.0 to project root
                    string? temp1 = Path.GetDirectoryName(currentDir);
                    string? temp2 = temp1 != null ? Path.GetDirectoryName(temp1) : null;
                    string? projectRoot = temp2 != null ? Path.GetDirectoryName(temp2) : null;
                    
                    if (projectRoot != null)
                    {
                        contentPath = Path.Combine(projectRoot, "Content", "Content.mgcb");
                    }
                    else
                    {
                        contentPath = Path.Combine(currentDir, "Content", "Content.mgcb");
                    }
                }
                else
                {
                    // Running from project directory or solution directory
                    contentPath = Path.Combine(currentDir, "BuildEditor", "Content", "Content.mgcb");
                    if (!File.Exists(contentPath))
                    {
                        contentPath = Path.Combine(currentDir, "Content", "Content.mgcb");
                    }
                }
                if (!File.Exists(contentPath))
                {
                    Console.WriteLine($"Content.mgcb not found at: {contentPath}");
                    return;
                }

                string[] lines = File.ReadAllLines(contentPath);
                
                foreach (string line in lines)
                {
                    if (line.StartsWith("#begin ") && line.EndsWith(".png"))
                    {
                        string textureName = line.Substring(7).Replace(".png", "");
                        
                        try
                        {
                            var texture = Content.Load<Texture2D>(textureName);
                            
                            // Categorize textures based on naming convention or folder structure
                            if (IsGeometryTexture(textureName))
                            {
                                _geometryTextures[textureName] = texture;
                                _loadedTextures[textureName] = texture; // Also add to legacy dictionary for compatibility
                                Console.WriteLine($"Loaded geometry texture: {textureName}");
                            }
                            else if (IsSpriteTexture(textureName))
                            {
                                _spriteTextures[textureName] = texture;
                                Console.WriteLine($"Loaded sprite texture: {textureName}");
                            }
                            else
                            {
                                // Default to geometry texture if unsure
                                _geometryTextures[textureName] = texture;
                                _loadedTextures[textureName] = texture;
                                Console.WriteLine($"Loaded texture (default to geometry): {textureName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to load texture '{textureName}': {ex.Message}");
                        }
                    }
                }
                
                Console.WriteLine($"Loaded {_geometryTextures.Count} geometry textures and {_spriteTextures.Count} sprite textures from Content.mgcb");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading Content.mgcb: {ex.Message}");
            }
        }

        private bool IsGeometryTexture(string textureName)
        {
            // Only textures in GeometryTextures folder are considered geometry textures
            return textureName.StartsWith("GeometryTextures/", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSpriteTexture(string textureName)
        {
            // Check if texture is in SpriteTextures folder or has sprite-related names
            if (textureName.StartsWith("SpriteTextures/", StringComparison.OrdinalIgnoreCase))
                return true;
                
            string lowerName = textureName.ToLower();
            return lowerName.Contains("sprite") || lowerName.Contains("enemy") || lowerName.Contains("pickup") || 
                   lowerName.Contains("player") || lowerName.Contains("item") || lowerName.Contains("decoration") ||
                   lowerName.Contains("light") || lowerName.Contains("switch");
        }

        private void LoadSpriteTextures()
        {
            // Create simple colored textures for different sprite types since we don't have actual sprite files
            // You can replace these with actual texture loading: Content.Load<Texture2D>("sprite_name")

            // Default sprite texture (white square)
            var defaultTexture = new Texture2D(GraphicsDevice, 16, 16);
            var defaultData = new Color[16 * 16];
            for (int i = 0; i < defaultData.Length; i++)
                defaultData[i] = Color.White;
            defaultTexture.SetData(defaultData);
            _spriteTextures["Default"] = defaultTexture;

            // Enemy sprite texture (red square with black border)
            var enemyTexture = new Texture2D(GraphicsDevice, 16, 16);
            var enemyData = new Color[16 * 16];
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x == 0 || x == 15 || y == 0 || y == 15)
                        enemyData[y * 16 + x] = Color.Black; // Border
                    else
                        enemyData[y * 16 + x] = Color.Red; // Fill
                }
            }

            enemyTexture.SetData(enemyData);
            _spriteTextures["Enemy"] = enemyTexture;

            // Pickup sprite texture (green circle)
            var pickupTexture = new Texture2D(GraphicsDevice, 16, 16);
            var pickupData = new Color[16 * 16];
            Vector2 center = new Vector2(8, 8);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= 7)
                        pickupData[y * 16 + x] = Color.Green;
                    else
                        pickupData[y * 16 + x] = Color.Transparent;
                }
            }

            pickupTexture.SetData(pickupData);
            _spriteTextures["Pickup"] = pickupTexture;
        }

        private void LoadWallTextures()
        {
            // Create a default white texture for fallback
            _defaultTexture = new Texture2D(GraphicsDevice, 64, 64);
            var defaultData = new Color[64 * 64];
            for (int i = 0; i < defaultData.Length; i++)
                defaultData[i] = Color.White;
            _defaultTexture.SetData(defaultData);

            // Auto-discover textures from Content directory
            DiscoverAndLoadTextures();

            // Add fallback procedural textures if no files found
            if (_loadedTextures.Count == 0)
            {
                LoadOrCreateTexture("White", Color.White);
                LoadOrCreateTexture("Gray", Color.Gray);
                LoadOrCreateTexture("LightGray", Color.LightGray);
                LoadOrCreateTexture("Brown", Color.SaddleBrown);
                LoadOrCreateTexture("DarkGreen", Color.DarkGreen);

                // Add more procedural textures
                LoadOrCreateBrickTexture("Brick", Color.Brown, Color.DarkRed);
                LoadOrCreateStoneTexture("Stone", Color.LightGray, Color.Gray);
                LoadOrCreateMetalTexture("Metal", Color.Silver, Color.DarkGray);
            }
        }

        private void DiscoverAndLoadTextures()
        {
            string contentPath = Path.Combine(Content.RootDirectory);
            if (!Directory.Exists(contentPath))
                return;

            string[] supportedExtensions = { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.tga" };
            
            foreach (string extension in supportedExtensions)
            {
                string[] files = Directory.GetFiles(contentPath, extension, SearchOption.AllDirectories);
                
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string relativePath = Path.GetRelativePath(contentPath, filePath);
                    string contentName = Path.ChangeExtension(relativePath, null).Replace(Path.DirectorySeparatorChar, '/');
                    
                    // Skip font files
                    if (fileName.ToLower().Contains("font"))
                        continue;
                        
                    LoadTextureFromContent(contentName, fileName);
                }
            }
        }

        private void LoadTextureFromContent(string contentName, string displayName)
        {
            try
            {
                Console.WriteLine($"Attempting to load texture: '{contentName}' as '{displayName}'");
                var texture = Content.Load<Texture2D>(contentName);
                _loadedTextures[displayName] = texture;
                Console.WriteLine($"Successfully loaded texture: '{displayName}'");
                
                // Add to texture colors dictionary with a default tint
                if (!_textureColors.ContainsKey(displayName))
                {
                    _textureColors[displayName] = Color.White;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture '{contentName}': {ex.Message}");
                // Create a placeholder colored texture as fallback
                CreateSolidTexture(displayName, GetRandomTextureColor());
            }
        }

        private Color GetRandomTextureColor()
        {
            var colors = new[] { Color.White, Color.Gray, Color.Brown, Color.DarkGreen, Color.LightGray };
            return colors[new Random().Next(colors.Length)];
        }

        private void LoadOrCreateTexture(string name, Color color)
        {
            try
            {
                // Try to load from Content pipeline first
                // _loadedTextures[name] = Content.Load<Texture2D>(name);

                // For now, create solid color texture
                CreateSolidTexture(name, color);
            }
            catch
            {
                // Fallback to solid color if file not found
                CreateSolidTexture(name, color);
            }
        }

        private void CreateSolidTexture(string name, Color color)
        {
            var texture = new Texture2D(GraphicsDevice, 64, 64);
            var data = new Color[64 * 64];
            for (int i = 0; i < data.Length; i++)
                data[i] = color;
            texture.SetData(data);
            _loadedTextures[name] = texture;
        }

        private void LoadOrCreateBrickTexture(string name, Color brickColor, Color mortarColor)
        {
            var texture = new Texture2D(GraphicsDevice, 64, 64);
            var data = new Color[64 * 64];

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Create brick pattern
                    bool isMortar = false;

                    // Horizontal mortar lines every 16 pixels
                    if (y % 16 == 0 || y % 16 == 1) isMortar = true;

                    // Vertical mortar lines, offset every other row
                    int offset = (y / 16) % 2 == 0 ? 0 : 32;
                    if ((x + offset) % 32 == 0 || (x + offset) % 32 == 1) isMortar = true;

                    data[y * 64 + x] = isMortar ? mortarColor : brickColor;
                }
            }

            texture.SetData(data);
            _loadedTextures[name] = texture;
        }

        private void LoadOrCreateStoneTexture(string name, Color lightStone, Color darkStone)
        {
            var texture = new Texture2D(GraphicsDevice, 64, 64);
            var data = new Color[64 * 64];
            var random = new Random(42); // Fixed seed for consistency

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Create stone pattern with random variation
                    float noise = (float)random.NextDouble();
                    Color color = Color.Lerp(lightStone, darkStone, noise);
                    data[y * 64 + x] = color;
                }
            }

            texture.SetData(data);
            _loadedTextures[name] = texture;
        }

        private void LoadOrCreateMetalTexture(string name, Color lightMetal, Color darkMetal)
        {
            var texture = new Texture2D(GraphicsDevice, 64, 64);
            var data = new Color[64 * 64];

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Create horizontal metal stripes
                    float stripe = (float)Math.Sin(y * 0.2f) * 0.5f + 0.5f;
                    Color color = Color.Lerp(darkMetal, lightMetal, stripe);
                    data[y * 64 + x] = color;
                }
            }

            texture.SetData(data);
            _loadedTextures[name] = texture;
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            // Update door animations with sector movement
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update lift animations
            foreach (var sector in _sectors)
            {
                if (sector.IsLift)
                {
                    UpdateLiftAnimation(sector, deltaTime);
                }
            }

            // Update 3D cursor for snapping and interaction
            Update3DCursor(mouseState);

            // Tab key to toggle 3D mode
            if (keyboardState.IsKeyDown(Keys.Tab) && !_previousKeyboardState.IsKeyDown(Keys.Tab))
                _is3DMode = !_is3DMode;
            
            // 2 key to toggle collision mode
            if (keyboardState.IsKeyDown(Keys.D2) && !_previousKeyboardState.IsKeyDown(Keys.D2))
            {
                _collisionMode = !_collisionMode;
                Console.WriteLine($"Collision mode: {(_collisionMode ? "ON" : "OFF")}");
            }
            

            // F key to toggle wireframe mode (only works in 3D mode)
            if (keyboardState.IsKeyDown(Keys.F) && !_previousKeyboardState.IsKeyDown(Keys.F) && _is3DMode)
                _wireframeMode = !_wireframeMode;

            // G key behavior depends on context
            if (keyboardState.IsKeyDown(Keys.G) && !_previousKeyboardState.IsKeyDown(Keys.G) && _is3DMode)
            {
                // If a sprite is selected, toggle mouse tracking mode for sprites
                if (_selectedSprite3D != null)
                {
                    _spriteMouseTrackMode = !_spriteMouseTrackMode;
                    
                    if (_spriteMouseTrackMode)
                    {
                        // Entering mouse tracking mode - store current position
                        _lastSpriteTrackPosition = _selectedSprite3D.Position;
                        _lastSpriteTrackHeight = _selectedSprite3D.Height;
                        _lastSpriteTrackAngle = _selectedSprite3D.Angle;
                        _lastSpriteTrackAlignment = _selectedSprite3D.Alignment;
                        Console.WriteLine($"Sprite mouse tracking mode: ON - Move mouse to position sprite");
                    }
                    else
                    {
                        // Exiting mouse tracking mode - restore to last valid tracking position if any valid position was set
                        if (_lastSpriteTrackPosition != Vector2.Zero)
                        {
                            _selectedSprite3D.Position = _lastSpriteTrackPosition;
                            _selectedSprite3D.Height = _lastSpriteTrackHeight;
                            _selectedSprite3D.Angle = _lastSpriteTrackAngle;
                            _selectedSprite3D.Alignment = _lastSpriteTrackAlignment;
                        }
                        Console.WriteLine($"Sprite mouse tracking mode: OFF - Sprite positioned at last tracked location");
                    }
                }
                else
                {
                    // Otherwise, toggle auto-alignment mode as before
                    _autoAlignMode = !_autoAlignMode;
                    Console.WriteLine($"Auto-alignment mode toggled: {_autoAlignMode}");
                }
            }

            // P key to toggle properties window (only when sector is selected)
            if (keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P) && _selectedSector != null)
                _propertiesWindowVisible = !_propertiesWindowVisible;

            // T key to toggle texture editor (only when sector is selected)
            if (keyboardState.IsKeyDown(Keys.T) && !_previousKeyboardState.IsKeyDown(Keys.T) && _selectedSector != null)
                _textureEditorVisible = !_textureEditorVisible;

            // S key to toggle sprite editor (only when sprite is selected)
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S) && _selectedSprite != null)
            {
                // Force open the sprite editor - don't just toggle, ensure it opens
                if (_spriteEditorVisible)
                    _spriteEditorVisible = false; // Close if open
                else
                    _spriteEditorVisible = true; // Open if closed
            }



            // C key to activate switches (test switch->door linking via HiTag)
            if (keyboardState.IsKeyDown(Keys.C) && !_previousKeyboardState.IsKeyDown(Keys.C))
                ActivateNearbyElements();

            // X key to deselect sprite in 3D mode (Escape exits program)
            if (keyboardState.IsKeyDown(Keys.X) && !_previousKeyboardState.IsKeyDown(Keys.X))
            {
                _selectedSprite3D = null;
                _selectedSprite = null;
            }

            // Arrow key rotation for selected sprite in 3D mode
            if (_selectedSprite3D != null && _is3DMode)
            {
                var rotationSpeed = 5f; // degrees per key press

                // Yaw rotation (Left/Right arrows) - turning around like a human
                if (keyboardState.IsKeyDown(Keys.Left) && !_previousKeyboardState.IsKeyDown(Keys.Left))
                {
                    _selectedSprite3D.Angle -= rotationSpeed;
                    Console.WriteLine($"Yaw left: new angle = {_selectedSprite3D.Angle}");
                }

                if (keyboardState.IsKeyDown(Keys.Right) && !_previousKeyboardState.IsKeyDown(Keys.Right))
                {
                    _selectedSprite3D.Angle += rotationSpeed;
                    Console.WriteLine($"Yaw right: new angle = {_selectedSprite3D.Angle}");
                }

                // Pitch rotation (Up/Down arrows) - tilting up/down like looking up/down
                if (keyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
                {
                    var currentPitch = _selectedSprite3D.Properties.ContainsKey("Pitch")
                        ? (float)_selectedSprite3D.Properties["Pitch"]
                        : 0f;
                    var newPitch = Math.Min(90f, currentPitch + rotationSpeed);
                    _selectedSprite3D.Properties["Pitch"] = newPitch;
                    Console.WriteLine($"Pitch up: new pitch = {newPitch}");
                }

                if (keyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
                {
                    var currentPitch = _selectedSprite3D.Properties.ContainsKey("Pitch")
                        ? (float)_selectedSprite3D.Properties["Pitch"]
                        : 0f;
                    var newPitch = Math.Max(-90f, currentPitch - rotationSpeed);
                    _selectedSprite3D.Properties["Pitch"] = newPitch;
                    Console.WriteLine($"Pitch down: new pitch = {newPitch}");
                }
            }

            // Numeric keys for quick height adjustments (only when sector selected and properties visible)
            if (_selectedSector != null && _propertiesWindowVisible)
            {
                // Floor height adjustments
                if (keyboardState.IsKeyDown(Keys.NumPad1) && !_previousKeyboardState.IsKeyDown(Keys.NumPad1))
                    AdjustFloorHeight(-1f);
                if (keyboardState.IsKeyDown(Keys.NumPad2) && !_previousKeyboardState.IsKeyDown(Keys.NumPad2))
                    AdjustFloorHeight(1f);

                // Ceiling height adjustments
                if (keyboardState.IsKeyDown(Keys.NumPad7) && !_previousKeyboardState.IsKeyDown(Keys.NumPad7))
                    AdjustCeilingHeight(-1f);
                if (keyboardState.IsKeyDown(Keys.NumPad8) && !_previousKeyboardState.IsKeyDown(Keys.NumPad8))
                    AdjustCeilingHeight(1f);
            }

            if (_is3DMode)
            {
                // Update 3D camera horizontal position to follow player if player position is set
                if (_hasPlayerPosition)
                {
                    _camera3DPosition.X = _playerPosition.X;
                    _camera3DPosition.Z = -_playerPosition.Y; // Convert 2D Y to 3D Z (negated)
                    
                    // Don't auto-set camera height - allow free vertical movement with Q/E
                }
                
                Handle3DCameraInput(keyboardState, mouseState);
                // Only handle 3D selection if not interacting with UI
                if (!_desktop.IsMouseOverGUI)
                    Handle3DSelection(mouseState);
            }
            else
            {
                HandleCameraInput(mouseState);
                HandleVertexPlacement(mouseState);
                HandleSelection(mouseState);
                HandleDelete(mouseState);
                HandleSpritePlace(mouseState);
                HandleSlopeEdit(mouseState);
            }

            UpdateUi();

            _previousMouseState = mouseState;
            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
        }

        private void UpdateUi()
        {
            _cameraLabel.Text = $"Camera: {_camera.Position.X:F1}, {_camera.Position.Y:F1}";
            _zoomLabel.Text = $"Zoom: {_camera.Zoom:F2}";
            _mouseLabel.Text = $"Mouse: {_mouseWorldPosition.X:F1}, {_mouseWorldPosition.Y:F1}";
            _sectorsLabel.Text = $"Count: {_sectors.Count}";

            if (_sectors.Count > 0)
            {
                var activeSector = _sectors[_sectors.Count - 1];
                _verticesLabel.Text = $"Vertices: {activeSector.Vertices.Count}";
                _wallsLabel.Text = $"Walls: {activeSector.Walls.Count}";
            }
            else
            {
                _verticesLabel.Text = "Vertices: 0";
                _wallsLabel.Text = "Walls: 0";
            }

            var autoAlignStatus = _autoAlignMode ? " | Auto-Align: ON" : "";
            _statusLabel.Text = $"World: {_mouseWorldPosition.X:F1}, {_mouseWorldPosition.Y:F1} | " +
                                $"Zoom: {_camera.Zoom:F2} | Grid: {GetGridSize():F0} | Mode: {_currentEditMode}{autoAlignStatus}";

            // Update radio button states
            _vertexModeButton.IsPressed = _currentEditMode == EditMode.VertexPlacement;
            _selectionModeButton.IsPressed = _currentEditMode == EditMode.Selection;
            _deleteModeButton.IsPressed = _currentEditMode == EditMode.Delete;
            _spritePlaceModeButton.IsPressed = _currentEditMode == EditMode.SpritePlace;
            _slopeModeButton.IsPressed = _currentEditMode == EditMode.SlopeEdit;

            // Update slope button visibility and states
            if (_floorSlopeButton != null && _ceilingSlopeButton != null)
            {
                bool slopeMode = _currentEditMode == EditMode.SlopeEdit;
                _floorSlopeButton.Visible = slopeMode;
                _ceilingSlopeButton.Visible = slopeMode;
                if (slopeMode)
                {
                    UpdateSlopeButtonStates();
                }
            }

            // Update sector mode button visibility and states
            if (_independentSectorButton != null && _nestedSectorButton != null)
            {
                bool vertexMode = _currentEditMode == EditMode.VertexPlacement;
                _independentSectorButton.Visible = vertexMode;
                _nestedSectorButton.Visible = vertexMode;
                if (vertexMode)
                {
                    UpdateSectorModeControls();
                }
            }


            // Update sector properties panel
            UpdateSectorPropertiesPanel();
            UpdateTextureEditor();
            UpdateSpriteEditor();
        }

        private void HandleCameraInput(MouseState mouseState)
        {
            var viewportBounds = GetViewportBounds();
            bool mouseInViewport = viewportBounds.Contains(mouseState.Position);

            if (!mouseInViewport)
                return;

            // Convert mouse position to viewport-relative coordinates
            var viewportMousePos = new Vector2(
                mouseState.Position.X - viewportBounds.X,
                mouseState.Position.Y - viewportBounds.Y);
            _mouseWorldPosition = _camera.ScreenToWorld(viewportMousePos);

            // Right-click drag camera movement
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                var mouseDelta = mouseState.Position.ToVector2() - _previousMouseState.Position.ToVector2();
                _camera.Position -= mouseDelta / _camera.Zoom;
            }

            var scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                var mouseWorldPosBefore = _camera.ScreenToWorld(viewportMousePos);
                _camera.Zoom *= (float)Math.Pow(1.1, scrollDelta / 120.0);
                _camera.Zoom = MathHelper.Clamp(_camera.Zoom, 0.1f, 10.0f);

                var mouseWorldPosAfter = _camera.ScreenToWorld(viewportMousePos);
                _camera.Position += mouseWorldPosBefore - mouseWorldPosAfter;
            }
        }

        private void HandleVertexPlacement(MouseState mouseState)
        {
            if (_currentEditMode != EditMode.VertexPlacement)
                return;

            // Don't handle input if mouse is over UI
            if (_desktop.IsMouseOverGUI)
            {
                _hoveredVertex = null;
                return;
            }

            var viewportBounds = GetViewportBounds();
            bool mouseInViewport = viewportBounds.Contains(mouseState.Position);

            if (!mouseInViewport)
            {
                _hoveredVertex = null;
                return;
            }

            var gridSize = GetGridSize();
            var snappedPos = new Vector2(
                (float)(Math.Round(_mouseWorldPosition.X / gridSize) * gridSize),
                (float)(Math.Round(_mouseWorldPosition.Y / gridSize) * gridSize));

            _hoveredVertex = snappedPos;

            if (mouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                Console.WriteLine($"✓ Left click detected at {snappedPos} (grid size: {gridSize})");
                
                
                // Check if a wall is selected - if so, add vertex to that wall's sector
                if (_selectedWall != null)
                {
                    var wallSector = FindSectorContainingWall(_selectedWall);
                    if (wallSector != null)
                    {
                        AddVertexToSelectedWall(wallSector, snappedPos);
                        return;
                    }
                }
                
                // Handle normal vertex placement for new sectors
                PlaceVertex(snappedPos);
            }
        }

        private Sector FindSectorContainingWall(Wall wall)
        {
            foreach (var sector in _sectors)
            {
                foreach (var sectorWall in sector.Walls)
                {
                    if (Vector2.Distance(sectorWall.Start, wall.Start) < 0.1f &&
                        Vector2.Distance(sectorWall.End, wall.End) < 0.1f)
                    {
                        return sector;
                    }
                }
            }
            return null;
        }

        private void AddVertexToSelectedWall(Sector sector, Vector2 newVertex)
        {
            // Check if the new vertex is close to any existing vertex - if so, snap to it
            float snapDistance = GetGridSize() * 0.5f;
            foreach (var existingVertex in sector.Vertices)
            {
                if (Vector2.Distance(newVertex, existingVertex) < snapDistance)
                {
                    Console.WriteLine($"Snapping new vertex {newVertex} to existing vertex {existingVertex}");
                    newVertex = existingVertex;
                    break;
                }
            }
            
            // Also check vertices from other sectors for snapping
            foreach (var otherSector in _sectors)
            {
                if (otherSector == sector) continue;
                
                foreach (var existingVertex in otherSector.Vertices)
                {
                    if (Vector2.Distance(newVertex, existingVertex) < snapDistance)
                    {
                        Console.WriteLine($"Snapping new vertex {newVertex} to existing vertex {existingVertex} from sector {otherSector.Id}");
                        newVertex = existingVertex;
                        break;
                    }
                }
            }
            
            // Find which wall index this selected wall corresponds to in the sector
            int wallIndex = -1;
            for (int i = 0; i < sector.Walls.Count; i++)
            {
                var wall = sector.Walls[i];
                if (Vector2.Distance(wall.Start, _selectedWall.Start) < 0.1f &&
                    Vector2.Distance(wall.End, _selectedWall.End) < 0.1f)
                {
                    wallIndex = i;
                    break;
                }
            }
            
            if (wallIndex >= 0)
            {
                // Insert the new vertex after the start vertex of the selected wall
                // For a wall from vertex[i] to vertex[i+1], insert at position i+1
                int insertIndex = (wallIndex + 1) % sector.Vertices.Count;
                sector.Vertices.Insert(insertIndex, newVertex);
                
                Console.WriteLine($"Inserted vertex {newVertex} at index {insertIndex} in sector {sector.Id}");
            }
            else
            {
                // Fallback - just add to end if wall not found
                sector.Vertices.Add(newVertex);
                Console.WriteLine($"Added vertex {newVertex} to end of sector {sector.Id} (wall not found)");
            }
            
            // Rebuild the sector's walls to incorporate the new vertex
            RebuildWallsForAllSectors();
            
            // Clear wall selection after adding vertex
            _selectedWall = null;
        }

        private Sector Get3DSectorUnderCursor(Ray ray)
        {
            float closestDistance = float.MaxValue;
            Sector closestSector = null;

            foreach (var sector in _sectors)
            {
                // Check if ray intersects with sector floor
                var floorPlane = new Plane(Vector3.Up, sector.FloorHeight);
                
                var intersection = ray.Intersects(floorPlane);
                if (intersection.HasValue)
                {
                    float distance = intersection.Value;
                    Vector3 intersectionPoint = ray.Position + ray.Direction * distance;
                    Vector2 point2D = new Vector2(intersectionPoint.X, -intersectionPoint.Z);
                    
                    // Check if the intersection point is inside this sector
                    if (IsPointInSector(point2D, sector) && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSector = sector;
                    }
                }
            }
            
            return closestSector;
        }

        private Sector FindSectorContainingPoint(Vector2 point)
        {
            foreach (var sector in _sectors)
            {
                if (IsPointInSector(point, sector))
                {
                    return sector;
                }
            }
            return null;
        }

        private void HandleSelection(MouseState mouseState)
        {
            if (_currentEditMode != EditMode.Selection)
                return;

            // Don't handle input if mouse is over UI
            if (_desktop.IsMouseOverGUI)
                return;

            var viewportBounds = GetViewportBounds();
            bool mouseInViewport = viewportBounds.Contains(mouseState.Position);

            if (!mouseInViewport)
                return;

            // Handle mouse press
            if (mouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                // First check for player selection
                if (_hasPlayerPosition && Vector2.Distance(_mouseWorldPosition, _playerPosition) <= 12f)
                {
                    _playerSelected = true;
                    _draggingPlayer = true;
                    _playerDragStart = _mouseWorldPosition;
                    _selectedSprite = null; // Clear sprite selection
                    _selectedSector = null; // Clear sector selection
                    _selectedVertices.Clear(); // Clear vertex selection
                    _selectedWall = null; // Clear wall selection
                    _spriteEditorVisible = false; // Hide sprite editor
                    _draggingSprite = false; // Stop sprite dragging
                    return;
                }

                // Then try to select a sprite
                var clickedSprite = GetSpriteAt(_mouseWorldPosition, 16f);
                if (clickedSprite != null)
                {
                    _selectedSprite = clickedSprite;
                    _selectedSector = null; // Clear sector selection
                    _selectedVertices.Clear(); // Clear vertex selection
                    _playerSelected = false; // Clear player selection
                    _spriteEditorVisible = true; // Show sprite editor

                    // Start sprite dragging
                    _draggingSprite = true;
                    _spriteDragStart = _mouseWorldPosition;
                    return;
                }

                // Check for vertex selection/drag start
                var clickedVertex = GetVertexAt(_mouseWorldPosition, 8f);
                if (clickedVertex.HasValue)
                {
                    // If vertex isn't selected, select it (clear others)
                    if (!_selectedVertices.Contains(clickedVertex.Value))
                    {
                        _selectedVertices.Clear();
                        _selectedVertices.Add(clickedVertex.Value);
                    }

                    // Start dragging
                    _isDragging = true;
                    _dragStart = clickedVertex.Value;
                    _selectedSector = null; // Clear sector selection
                    _selectedSprite = null; // Clear sprite selection
                    _playerSelected = false; // Clear player selection
                    _spriteEditorVisible = false; // Hide sprite editor
                    _draggingSprite = false; // Stop sprite dragging
                    return;
                }

                // Check for wall selection 
                var clickedWall = GetWallNearPoint(_mouseWorldPosition, GetGridSize() * 0.5f);
                if (clickedWall.HasValue)
                {
                    _selectedWall = clickedWall.Value.sector.Walls[clickedWall.Value.wallIndex];
                    _selectedSector = null; // Clear sector selection
                    _selectedVertices.Clear(); // Clear vertex selection
                    _selectedSprite = null; // Clear sprite selection
                    _playerSelected = false; // Clear player selection
                    _spriteEditorVisible = false; // Hide sprite editor
                    return;
                }

                // Check if clicking on a sector (only in Selection mode)
                if (_currentEditMode == EditMode.Selection)
                {
                    var clickedSector = GetSectorAt(_mouseWorldPosition);
                    if (clickedSector != null)
                    {
                        _selectedSector = clickedSector;
                        _selectedWall = null; // Clear wall selection
                        _selectedVertices.Clear(); // Clear vertex selection
                        _selectedSprite = null; // Clear sprite selection
                        _playerSelected = false; // Clear player selection
                        _spriteEditorVisible = false; // Hide sprite editor
                    }
                }
                else
                {
                    // Clear all selections if clicking on empty space
                    _selectedVertices.Clear();
                    _selectedSprite = null; // Clear sprite selection
                    _spriteEditorVisible = false; // Hide sprite editor
                    _selectedSector = null;
                    _selectedWall = null;
                    _playerSelected = false; // Clear player selection
                }
            }

            // Handle dragging
            if (_isDragging && mouseState.LeftButton == ButtonState.Pressed)
            {
                if (_dragStart.HasValue && _selectedVertices.Count > 0)
                {
                    // Calculate drag offset
                    Vector2 dragOffset = _mouseWorldPosition - _dragStart.Value;

                    // Move all selected vertices
                    var verticesToMove = new List<Vector2>(_selectedVertices);
                    _selectedVertices.Clear();

                    foreach (var vertex in verticesToMove)
                    {
                        Vector2 newPos = vertex + dragOffset;

                        // Update vertex in all sectors that contain it
                        foreach (var sector in _sectors)
                        {
                            for (int i = 0; i < sector.Vertices.Count; i++)
                            {
                                if (Vector2.Distance(sector.Vertices[i], vertex) < 0.1f)
                                {
                                    sector.Vertices[i] = newPos;
                                }
                            }
                        }

                        _selectedVertices.Add(newPos);
                    }

                    _dragStart = _mouseWorldPosition; // Update drag start for next frame
                }
            }

            // Handle player dragging
            if (_draggingPlayer && mouseState.LeftButton == ButtonState.Pressed)
            {
                // Calculate drag offset and move player
                Vector2 dragOffset = _mouseWorldPosition - _playerDragStart;
                _playerPosition += dragOffset;
                _playerDragStart = _mouseWorldPosition; // Update drag start for next frame
            }

            // Handle sprite dragging
            if (_draggingSprite && mouseState.LeftButton == ButtonState.Pressed && _selectedSprite != null)
            {
                // Calculate drag offset and move sprite
                Vector2 dragOffset = _mouseWorldPosition - _spriteDragStart;
                _selectedSprite.Position += dragOffset;
                _spriteDragStart = _mouseWorldPosition; // Update drag start for next frame
            }

            // End dragging
            if (_isDragging && mouseState.LeftButton == ButtonState.Released)
            {
                _isDragging = false;
                _dragStart = null;

                // Rebuild walls for all sectors after vertex movement
                RebuildWallsForAllSectors();
            }

            // End player dragging
            if (_draggingPlayer && mouseState.LeftButton == ButtonState.Released)
            {
                _draggingPlayer = false;
            }

            if (_draggingSprite && mouseState.LeftButton == ButtonState.Released)
            {
                _draggingSprite = false;
            }
        }

        private void RebuildWallsForAllSectors()
        {
            foreach (var sector in _sectors)
            {
                // Store nested sector properties
                bool wasNested = sector.IsNested;
                int? parentSectorId = sector.ParentSectorId;

                // Rebuild walls
                sector.Walls.Clear();
                for (int i = 0; i < sector.Vertices.Count; i++)
                {
                    int nextIndex = (i + 1) % sector.Vertices.Count;
                    var wall = new Wall
                    {
                        Start = sector.Vertices[i],
                        End = sector.Vertices[nextIndex]
                    };


                    sector.Walls.Add(wall);
                }

                // Restore nested sector properties
                if (wasNested)
                {
                    sector.IsNested = true;
                    sector.ParentSectorId = parentSectorId;
                }
            }

            // After rebuilding walls, detect nested sectors if in nested mode, then shared edges
            if (_createNestedSector)
            {
                DetectNestedSectors();
            }
            DetectSharedEdges();
        }

        private void DetectNestedSectors()
        {
            // Check each sector to see if it's inside another sector
            foreach (var sector in _sectors)
            {
                // Skip sectors that are already marked as nested
                if (sector.IsNested)
                    continue;

                // Check if this sector is inside any other sector
                var parentSector = FindParentSectorForPoint(GetCenterOfVertices(sector.Vertices));
                if (parentSector != null && parentSector != sector)
                {
                    Console.WriteLine($"Detected sector {sector.Id} as nested inside sector {parentSector.Id}");
                    sector.IsNested = true;
                    sector.ParentSectorId = parentSector.Id;
                }
            }
        }

        private void DetectSharedEdges()
        {
            Console.WriteLine("Running shared edge detection...");
            // Reset all walls to single-sided first
            foreach (var sector in _sectors)
            {
                foreach (var wall in sector.Walls)
                {
                    wall.IsTwoSided = false;
                    wall.AdjacentSectorId = null;
                }
            }

            // Check each pair of independent sectors for shared edges
            for (int i = 0; i < _sectors.Count; i++)
            {
                for (int j = i + 1; j < _sectors.Count; j++)
                {
                    var sectorA = _sectors[i];
                    var sectorB = _sectors[j];

                    // Only check independent sectors (not nested sectors)
                    if (sectorA.IsNested || sectorB.IsNested)
                        continue;

                    // Check each wall in sector A against each wall in sector B
                    foreach (var wallA in sectorA.Walls)
                    {
                        foreach (var wallB in sectorB.Walls)
                        {
                            // Check if walls share the same edge (vertex-to-vertex)
                            if (AreWallsShared(wallA, wallB))
                            {
                                // Mark both walls as two-sided and reference each other's sectors
                                wallA.IsTwoSided = true;
                                wallA.AdjacentSectorId = sectorB.Id;
                                wallB.IsTwoSided = true;
                                wallB.AdjacentSectorId = sectorA.Id;

                                // Debug output to console
                                Console.WriteLine(
                                    $"Found shared edge between sector {sectorA.Id} and sector {sectorB.Id}");
                            }
                        }
                    }
                }
            }
        }

        private bool AreWallsShared(Wall wallA, Wall wallB)
        {
            // Check if the walls share the same edge (allowing for reversed direction)
            const float tolerance = 5.0f; // Increased tolerance for manual drawing

            // Case 1: Same direction (A.Start -> A.End matches B.Start -> B.End)
            bool sameDirection = Vector2.Distance(wallA.Start, wallB.Start) < tolerance &&
                                 Vector2.Distance(wallA.End, wallB.End) < tolerance;

            // Case 2: Opposite direction (A.Start -> A.End matches B.End -> B.Start)
            bool oppositeDirection = Vector2.Distance(wallA.Start, wallB.End) < tolerance &&
                                     Vector2.Distance(wallA.End, wallB.Start) < tolerance;

            return sameDirection || oppositeDirection;
        }

        private Vector2? GetVertexAt(Vector2 worldPos, float radius)
        {
            foreach (var sector in _sectors)
            {
                foreach (var vertex in sector.Vertices)
                {
                    if (Vector2.Distance(worldPos, vertex) <= radius)
                        return vertex;
                }
            }

            return null;
        }

        private (Sector sector, int wallIndex)? GetWallNearPoint(Vector2 point, float tolerance)
        {
            foreach (var sector in _sectors)
            {
                for (int i = 0; i < sector.Walls.Count; i++)
                {
                    var wall = sector.Walls[i];
                    float distance = DistanceToLineSegment(point, wall.Start, wall.End);
                    if (distance <= tolerance)
                    {
                        return (sector, i);
                    }
                }
            }

            return null;
        }

        private bool IsSectorClosed(Sector sector)
        {
            // A sector is closed if it has at least 3 vertices and a closing wall that connects the last vertex to the first
            if (sector.Vertices.Count < 3)
                return false;

            // Check if there's a wall connecting the last vertex to the first vertex
            var firstVertex = sector.Vertices[0];
            var lastVertex = sector.Vertices[sector.Vertices.Count - 1];

            return sector.Walls.Any(wall =>
                (Vector2.Distance(wall.Start, lastVertex) < 0.1f && Vector2.Distance(wall.End, firstVertex) < 0.1f) ||
                (Vector2.Distance(wall.Start, firstVertex) < 0.1f && Vector2.Distance(wall.End, lastVertex) < 0.1f));
        }


        private void InsertVertexOnWall(Sector sector, int wallIndex, Vector2 newVertexPos)
        {
            // Find the vertex index where we should insert the new vertex
            var wall = sector.Walls[wallIndex];

            // Find the indices of the wall's start and end vertices
            int startVertexIndex = -1;
            int endVertexIndex = -1;

            for (int i = 0; i < sector.Vertices.Count; i++)
            {
                if (Vector2.Distance(sector.Vertices[i], wall.Start) < 0.1f)
                    startVertexIndex = i;
                if (Vector2.Distance(sector.Vertices[i], wall.End) < 0.1f)
                    endVertexIndex = i;
            }

            if (startVertexIndex != -1 && endVertexIndex != -1)
            {
                // Insert the new vertex after the start vertex
                int insertIndex = Math.Max(startVertexIndex, endVertexIndex);
                if (startVertexIndex > endVertexIndex)
                    insertIndex = startVertexIndex;
                else
                    insertIndex = endVertexIndex;

                sector.Vertices.Insert(insertIndex, newVertexPos);

                // Rebuild walls for this sector to incorporate the new vertex
                RebuildWallsForSector(sector);
            }
        }


        private void PlaceVertex(Vector2 position)
        {
            if (_sectors.Count == 0)
            {
                _sectors.Add(new Sector(_nextSectorId++));
            }
            
            // If the last sector is closed, create a new one for fresh polygon buffer
            if (IsSectorClosed(_sectors[_sectors.Count - 1]))
            {
                _sectors.Add(new Sector(_nextSectorId++));
            }
            
            var currentSector = _sectors[_sectors.Count - 1];
            currentSector.Vertices.Add(position);
            
            // Create walls between vertices
            if (currentSector.Vertices.Count >= 2)
            {
                currentSector.Walls.Add(new Wall 
                { 
                    Start = currentSector.Vertices[currentSector.Vertices.Count - 2],
                    End = currentSector.Vertices[currentSector.Vertices.Count - 1]
                });
            }
            
            // Check if we should close the sector (click near first vertex)
            if (currentSector.Vertices.Count >= 4)
            {
                var distToFirst = Vector2.Distance(position, currentSector.Vertices[0]);
                if (distToFirst < GetGridSize() * 2)
                {
                    // Close the loop by adding final wall from last to first vertex
                    currentSector.Walls.Add(new Wall 
                    { 
                        Start = currentSector.Vertices[currentSector.Vertices.Count - 1],
                        End = currentSector.Vertices[0]
                    });
                    
                    // Remove the duplicate vertex since we're closing the loop
                    currentSector.Vertices.RemoveAt(currentSector.Vertices.Count - 1);
                    
                    // Create new sector for next drawing
                    _sectors.Add(new Sector(_nextSectorId++));
                    
                    // Detect shared walls between sectors
                    RebuildWallsForAllSectors();
                }
            }
        }

        private Rectangle GetViewportBounds()
        {
            return new Rectangle(250, 0, GraphicsDevice.Viewport.Width - 250, GraphicsDevice.Viewport.Height);
        }

        private float GetGridSize()
        {
            var baseGridSize = 32f;
            if (_camera.Zoom < 0.5f) return baseGridSize * 4;
            if (_camera.Zoom < 1.0f) return baseGridSize * 2;
            if (_camera.Zoom > 3.0f) return baseGridSize / 2;
            return baseGridSize;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (_is3DMode)
            {
                Draw3DViewport();
            }
            else
            {
                DrawViewport();
            }

            DrawUi();

            base.Draw(gameTime);
        }

        private void DrawUi()
        {
            _desktop.Render();
        }

        private void DrawViewport()
        {
            var viewportBounds = GetViewportBounds();
            var originalViewport = GraphicsDevice.Viewport;

            GraphicsDevice.Viewport = new Viewport(viewportBounds.X, viewportBounds.Y,
                viewportBounds.Width, viewportBounds.Height);

            GraphicsDevice.Clear(Color.DarkSlateGray);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

            DrawGrid();
            DrawSectors();
            DrawVertexPlacementFeedback();
            DrawSelectionFeedback();

            _spriteBatch.End();

            GraphicsDevice.Viewport = originalViewport;
        }

        private void DrawGrid()
        {
            var gridSize = GetGridSize();
            var viewBounds = _camera.GetViewBounds(GetViewportBounds());

            // Ensure grid size is reasonable to prevent excessive line drawing
            if (gridSize <= 0 || gridSize > 1000) return;

            // Calculate grid bounds with proper floating-point math and padding
            var startX = (float)(Math.Floor(viewBounds.X / gridSize) * gridSize) - gridSize;
            var startY = (float)(Math.Floor(viewBounds.Y / gridSize) * gridSize) - gridSize;
            var endX = viewBounds.X + viewBounds.Width + gridSize;
            var endY = viewBounds.Y + viewBounds.Height + gridSize;

            var gridColor = Color.Gray * 0.3f;

            // Limit number of lines to prevent performance issues
            var maxLines = 1600;
            var lineCount = 0;

            for (float x = startX; x <= endX && lineCount < maxLines; x += gridSize, lineCount++)
            {
                DrawLine(new Vector2(x, startY),
                    new Vector2(x, endY), gridColor, 1);
            }

            lineCount = 0;
            for (float y = startY; y <= endY && lineCount < maxLines; y += gridSize, lineCount++)
            {
                DrawLine(new Vector2(startX, y),
                    new Vector2(endX, y), gridColor, 1);
            }
        }

        private void DrawSectors()
        {
            foreach (var sector in _sectors)
            {
                foreach (var wall in sector.Walls)
                {
                    // Determine wall color and thickness based on sector type and two-sided status
                    Color wallColor = GetWallColorForSector(sector);
                    int wallThickness = sector.IsNested ? 1 : 2; // Thinner lines for nested sectors

                    // Two-sided walls get a different visual treatment
                    if (wall.IsTwoSided)
                    {
                        wallColor = Color.Cyan; // Two-sided walls are cyan
                        wallThickness = 1; // Thinner for two-sided walls
                    }

                    // In slope mode, show wall height gradients (only for selected sector)
                    if (_currentEditMode == EditMode.SlopeEdit && sector == _selectedSector && sector.HasSlopes)
                    {
                        var startVertexIndex = sector.Vertices.IndexOf(wall.Start);
                        var endVertexIndex = sector.Vertices.IndexOf(wall.End);

                        if (startVertexIndex >= 0 && endVertexIndex >= 0)
                        {
                            var startHeight = GetVertexHeight(sector, startVertexIndex, _isEditingFloorSlope);
                            var endHeight = GetVertexHeight(sector, endVertexIndex, _isEditingFloorSlope);

                            // Color gradient based on height difference
                            var heightDiff = Math.Abs(endHeight - startHeight);
                            wallColor = heightDiff > 1f ? Color.Orange : wallColor;
                            wallThickness = heightDiff > 1f ? 3 : wallThickness;

                            DrawLine(wall.Start, wall.End, wallColor, wallThickness);
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        // Draw walls with door sections removed
                        DrawLine(wall.Start, wall.End, wallColor, wallThickness);
                    }
                }

                // Draw vertices in each sector
                foreach (var vertex in sector.Vertices)
                {
                    var vertexIndex = sector.Vertices.IndexOf(vertex);

                    if (_currentEditMode == EditMode.SlopeEdit && sector == _selectedSector)
                    {
                        // In slope mode, show vertex heights with color coding (only for selected sector)
                        var vertexHeight = GetVertexHeight(sector, vertexIndex, _isEditingFloorSlope);
                        var baseHeight = _isEditingFloorSlope ? sector.FloorHeight : sector.CeilingHeight;
                        var heightDiff = vertexHeight - baseHeight;

                        // Color code vertices based on height difference
                        Color vertexColor = Color.Yellow;
                        if (heightDiff > 0.1f) vertexColor = Color.Red; // Above base height
                        else if (heightDiff < -0.1f) vertexColor = Color.Blue; // Below base height

                        var radius = vertex == _selectedVertex ? 8f : 6f; // Larger if selected
                        DrawCircle(vertex, radius, vertexColor);

                    }
                    else
                    {
                        // Use different vertex colors for nested sectors
                        var vertexColor = sector.IsNested ? GetWallColorForSector(sector) : Color.Yellow;
                        var radius = sector.IsNested ? 3f : 4f; // Smaller vertices for nested sectors
                        DrawCircle(vertex, radius, vertexColor);
                    }
                }

                // Draw sprites in this sector
                foreach (var sprite in sector.Sprites)
                {
                    DrawSprite2D(sprite);
                }
            }


        }

        private void DrawSprite2D(Sprite sprite)
        {
            if (!sprite.Visible) return;

            // Get texture for sprite (use TextureName or fallback based on tag)
            string textureName = sprite.TextureName;

            // If sprite doesn't have a specific texture, choose based on tag
            if (string.IsNullOrEmpty(textureName) || !_spriteTextures.ContainsKey(textureName))
            {
                textureName = "Default";
            }

            // Get the texture
            if (_spriteTextures.TryGetValue(textureName, out Texture2D texture))
            {
                // Calculate sprite size with scale
                float scaleX = sprite.Scale.X;
                float scaleY = sprite.Scale.Y;

                // Tint color based on sprite tag for visibility
                Color tintColor = sprite.Tag switch
                {
                    SpriteTag.Switch => Color.Magenta,
                    SpriteTag.Decoration => Color.Cyan,
                    _ => Color.White
                };

                // Draw sprite with rotation and scale
                Vector2 origin = new Vector2(texture.Width / 2, texture.Height / 2);
                float rotation = MathHelper.ToRadians(sprite.Angle);
                Vector2 scale = new Vector2(scaleX, scaleY);

                _spriteBatch.Draw(texture, sprite.Position, null, tintColor, rotation, origin, scale,
                    SpriteEffects.None, 0f);
            }
            else
            {
                // Fallback to colored shapes if texture not found
                Color fallbackColor = sprite.Tag switch
                {
                    SpriteTag.Switch => Color.Magenta,
                    SpriteTag.Decoration => Color.Cyan,
                    _ => Color.White
                };

                // Choose shape based on alignment
                switch (sprite.Alignment)
                {
                    case SpriteAlignment.Floor:
                        var floorRect = new Rectangle(
                            (int)(sprite.Position.X - 8),
                            (int)(sprite.Position.Y - 8),
                            16, 16);
                        _spriteBatch.Draw(_pixelTexture, floorRect, fallbackColor);
                        break;

                    case SpriteAlignment.Wall:
                        var wallRect = new Rectangle(
                            (int)(sprite.Position.X - 4),
                            (int)(sprite.Position.Y - 12),
                            8, 24);
                        _spriteBatch.Draw(_pixelTexture, wallRect, fallbackColor);
                        break;

                    case SpriteAlignment.Face:
                    default:
                        DrawCircle(sprite.Position, 8, fallbackColor);
                        break;
                }
            }

            // Draw selection highlight
            if (_selectedSprite == sprite)
            {
                DrawCircle(sprite.Position, 12, Color.White * 0.7f);
            }

            // Draw direction indicator for angled sprites
            if (sprite.Angle != 0)
            {
                var direction = new Vector2(
                    (float)Math.Cos(sprite.Angle * Math.PI / 180) * 15,
                    (float)Math.Sin(sprite.Angle * Math.PI / 180) * 15);
                var endPos = sprite.Position + direction;
                DrawLine(sprite.Position, endPos, Color.White, 1);
            }
        }


        private void DrawVertexPlacementFeedback()
        {
            if (_currentEditMode == EditMode.VertexPlacement && _hoveredVertex.HasValue)
            {
                DrawCircle(_hoveredVertex.Value, 6, Color.LightGreen);
            }
        }

        private void HandleDelete(MouseState mouseState)
        {
            if (_currentEditMode != EditMode.Delete)
                return;

            // Don't handle input if mouse is over UI
            if (_desktop.IsMouseOverGUI)
                return;

            var viewportBounds = GetViewportBounds();
            bool mouseInViewport = viewportBounds.Contains(mouseState.Position);

            if (!mouseInViewport)
                return;

            if (mouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                // Try to delete a sprite
                var clickedSprite = GetSpriteAt(_mouseWorldPosition, 16f);
                if (clickedSprite != null)
                {
                    // Find and remove sprite from its sector
                    foreach (var sector in _sectors)
                    {
                        if (sector.Sprites.Remove(clickedSprite))
                        {
                            if (_selectedSprite == clickedSprite)
                                _selectedSprite = null;
                            return;
                        }
                    }
                }

                // Finally try to delete a vertex
                var clickedVertex = GetVertexAt(_mouseWorldPosition, 8f);
                if (clickedVertex.HasValue)
                {
                    DeleteVertex(clickedVertex.Value);
                }
            }
        }

        private void DeleteVertex(Vector2 vertexToDelete)
        {
            for (int sectorIndex = 0; sectorIndex < _sectors.Count; sectorIndex++)
            {
                var sector = _sectors[sectorIndex];

                // Find and remove the vertex
                for (int vertexIndex = 0; vertexIndex < sector.Vertices.Count; vertexIndex++)
                {
                    if (Vector2.Distance(sector.Vertices[vertexIndex], vertexToDelete) < 1f)
                    {
                        sector.Vertices.RemoveAt(vertexIndex);

                        // Rebuild walls for this sector
                        RebuildWallsForSector(sector);

                        // Remove from selected vertices if it was selected
                        _selectedVertices.Remove(vertexToDelete);

                        // If sector has less than 3 vertices, remove it entirely
                        if (sector.Vertices.Count < 3)
                        {
                            _sectors.RemoveAt(sectorIndex);
                        }

                        return;
                    }
                }
            }
        }

        private void RebuildWallsForSector(Sector sector)
        {
            sector.Walls.Clear();

            if (sector.Vertices.Count < 2)
                return;

            // Create walls between consecutive vertices
            for (int i = 0; i < sector.Vertices.Count - 1; i++)
            {
                sector.Walls.Add(new Wall
                {
                    Start = sector.Vertices[i],
                    End = sector.Vertices[i + 1]
                });
            }

            // Close the sector if it has 3 or more vertices
            if (sector.Vertices.Count >= 3)
            {
                sector.Walls.Add(new Wall
                {
                    Start = sector.Vertices[sector.Vertices.Count - 1],
                    End = sector.Vertices[0]
                });
            }
        }


        private void HandleSpritePlace(MouseState mouseState)
        {
            if (_currentEditMode != EditMode.SpritePlace)
                return;

            // Don't handle input if mouse is over UI
            if (_desktop.IsMouseOverGUI)
                return;

            var viewportBounds = GetViewportBounds();
            bool mouseInViewport = viewportBounds.Contains(mouseState.Position);

            if (!mouseInViewport)
                return;

            // Update mouse world position for sprite placement (ensure it's current)
            var viewportMousePos = new Vector2(
                mouseState.Position.X - viewportBounds.X,
                mouseState.Position.Y - viewportBounds.Y);
            var mouseWorldPos = _camera.ScreenToWorld(viewportMousePos);

            // Real-time slope collision testing for mouse position
            var currentSector = GetSectorAt(mouseWorldPos);
            if (currentSector != null)
            {
                // Test Build engine slope collision at current mouse position
                GetZsOfSlope(currentSector, mouseWorldPos.X, mouseWorldPos.Y, out float ceilZ, out float florZ);
                Console.WriteLine($"MOUSE SLOPE: Sector {currentSector.Id} at ({mouseWorldPos.X:F1},{mouseWorldPos.Y:F1}) -> Floor={florZ:F1}, Ceiling={ceilZ:F1} (sloped: floor={currentSector.HasSlopes}, ceiling={currentSector.HasSlopes})");
            }

            // Test wall collision for independent sectors when mouse moves
            TestWallCollisionAtMouse(mouseWorldPos);

            if (mouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                // Place sprite at mouse position
                var clickedSector = GetSectorAt(mouseWorldPos);
                if (clickedSector != null)
                {
                    var newSprite = new Sprite
                    {
                        Id = _nextSpriteId++,
                        Position = mouseWorldPos,
                        SectorId = clickedSector.Id,
                        TextureName = "Default",
                        Tag = SpriteTag.Decoration,
                        Alignment = SpriteAlignment.Face,
                        Height = 32f // Set reasonable default height above floor
                    };

                    clickedSector.Sprites.Add(newSprite);
                    _selectedSprite = newSprite;
                }
            }

            // Right-click to select existing sprite
            if (mouseState.RightButton == ButtonState.Pressed &&
                _previousMouseState.RightButton == ButtonState.Released)
            {
                var clickedSprite = GetSpriteAt(mouseWorldPos, 16f); // 16 pixel tolerance
                _selectedSprite = clickedSprite;
                // Don't modify _spriteEditorVisible here - let the S key handle it
            }
        }

        private Sprite GetSpriteAt(Vector2 worldPos, float tolerance)
        {
            foreach (var sector in _sectors)
            {
                foreach (var sprite in sector.Sprites)
                {
                    if (Vector2.Distance(sprite.Position, worldPos) <= tolerance)
                    {
                        return sprite;
                    }
                }
            }

            return null;
        }

        private Wall GetWallAt(Vector2 worldPos, float tolerance)
        {
            foreach (var sector in _sectors)
            {
                foreach (var wall in sector.Walls)
                {
                    var distance = DistanceToLineSegment(worldPos, wall.Start, wall.End);
                    if (distance <= tolerance)
                        return wall;
                }
            }

            return null;
        }

        private float DistanceToLineSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var length = Vector2.Distance(start, end);
            if (length == 0) return Vector2.Distance(point, start);

            var t = Math.Max(0, Math.Min(1, Vector2.Dot(point - start, end - start) / (length * length)));
            var projection = start + t * (end - start);
            return Vector2.Distance(point, projection);
        }

        private Sector GetSectorAt(Vector2 worldPos)
        {
            // Build engine style sector detection with platform collision support
            // Independent sectors inside other sectors should act as solid platforms/ramps
            
            List<Sector> containingSectors = new List<Sector>();
            
            // Find all sectors that contain this point
            foreach (var sector in _sectors)
            {
                if (IsPointInPolygon(worldPos, sector.Vertices))
                {
                    containingSectors.Add(sector);
                }
            }
            
            if (containingSectors.Count == 0)
                return null;
                
            if (containingSectors.Count == 1)
                return containingSectors[0];
            
            // Multiple sectors contain the point - need to determine collision priority
            // For independent platforms/ramps: use the sector with highest floor collision
            var candidateSector = GetBestCollisionSector(worldPos, containingSectors);
            
            // Debug output to show sector selection logic
            var areas = containingSectors.Select(s => CalculatePolygonArea(s.Vertices)).ToList();
            var sectorInfo = string.Join(", ", containingSectors.Select((s, i) => $"Sector{s.Id}(area={areas[i]:F1})"));
            Console.WriteLine($"Found {containingSectors.Count} sectors at position {worldPos}: {sectorInfo} -> Selected: Sector{candidateSector.Id}");
            
            return candidateSector;
        }

        private Sector GetBestCollisionSector(Vector2 worldPos, List<Sector> containingSectors)
        {
            // For independent platforms/ramps inside other sectors:
            // Priority: highest floor collision (smallest sector area = platform)
            
            float bestScore = float.MinValue;
            Sector bestSector = containingSectors[0];
            
            foreach (var sector in containingSectors)
            {
                float score = CalculateSectorCollisionScore(worldPos, sector);
                Console.WriteLine($"  Sector {sector.Id} collision score: {score:F2}");
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSector = sector;
                }
            }
            
            return bestSector;
        }
        
        private float CalculateSectorCollisionScore(Vector2 worldPos, Sector sector)
        {
            // Calculate collision priority score for independent platforms/ramps
            // Higher score = higher priority for collision
            
            float area = CalculatePolygonArea(sector.Vertices);
            GetZsOfSlope(sector, worldPos.X, worldPos.Y, out float ceilZ, out float florZ);
            
            // Smaller area (platforms/ramps) gets higher priority
            float areaPriority = 1.0f / Math.Max(area, 1.0f);
            
            // Higher floor gets slightly higher priority (for stacked platforms)
            float heightPriority = florZ * 0.01f;
            
            // Combine factors: area is primary, height is secondary
            float totalScore = areaPriority * 1000.0f + heightPriority;
            
            return totalScore;
        }

        private Vector2 GetAdjustedMovement(Vector2 fromPos, Vector2 movement)
        {
            // Build engine collision: treat player as a circle, check against all sector walls
            Vector2 newPos = fromPos + movement;
            Vector2 adjustedMovement = movement;
            
            // Check collision against all sectors (including independent sectors)
            foreach (var sector in _sectors)
            {
                adjustedMovement = CheckCircleCollisionWithSector(fromPos, adjustedMovement, sector);
            }
            
            return adjustedMovement;
        }
        
        private Vector2 CheckCircleCollisionWithSector(Vector2 fromPos, Vector2 movement, Sector sector)
        {
            if (sector.Vertices.Count < 3) return movement;

            // ONLY check collision for independent sectors - preserves nested sector portals
            if (sector.SectorType != SectorType.Independent)
                return movement;

            Vector2 toPos = fromPos + movement;
            Vector2 adjustedMovement = movement;

            // For independent sectors, ALWAYS check collision regardless of player position
            // This ensures collision works from both inside and outside (both sides of walls)
            for (int i = 0; i < sector.Vertices.Count; i++)
            {
                int nextIndex = (i + 1) % sector.Vertices.Count;
                Vector2 wallStart = sector.Vertices[i];
                Vector2 wallEnd = sector.Vertices[nextIndex];

                // Check if movement path intersects with this wall considering player radius
                if (DoesCirclePathIntersectWall(fromPos, toPos, PLAYER_RADIUS, wallStart, wallEnd))
                {
                    // Collision detected - project movement along the wall (sliding)
                    Vector2 wallVec = wallEnd - wallStart;
                    float wallLength = wallVec.Length();
                    Vector2 wallDir = wallLength > 0 ? wallVec / wallLength : Vector2.Zero;

                    // Project movement onto wall direction for sliding
                    float projectionLength = Vector2.Dot(adjustedMovement, wallDir);
                    adjustedMovement = wallDir * projectionLength;

                    break; // Handle one collision at a time
                }
            }

            return adjustedMovement;
        }
        
        private bool DoesCirclePathIntersectWall(Vector2 fromPos, Vector2 toPos, float radius, Vector2 wallStart, Vector2 wallEnd)
        {
            // Simplified collision check: test if either position or any point along the path is too close to the wall
            // This is simpler and matches the working approach used in the 3D collision system
            
            // Check start position
            float startDistance = DistanceFromPointToLineSegment(fromPos, wallStart, wallEnd);
            if (startDistance <= radius) return true;
            
            // Check end position 
            float endDistance = DistanceFromPointToLineSegment(toPos, wallStart, wallEnd);
            if (endDistance <= radius) return true;
            
            // For more accurate path collision, check a few points along the movement path
            for (float t = 0.2f; t < 1.0f; t += 0.2f)
            {
                Vector2 pathPoint = Vector2.Lerp(fromPos, toPos, t);
                float pathDistance = DistanceFromPointToLineSegment(pathPoint, wallStart, wallEnd);
                if (pathDistance <= radius) return true;
            }
            
            return false;
        }
        
        // Removed the complex DistanceFromLineToLineSegment method
        // Using the simpler DistanceFromPointToLineSegment approach instead
        
        private float DistanceFromPointToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            // Use the same approach as the working PointToLineDistance method from the 3D collision system
            Vector2 line = lineEnd - lineStart;
            Vector2 toPoint = point - lineStart;
            
            float lineLength = line.Length();
            if (lineLength == 0) return Vector2.Distance(point, lineStart);
            
            // Project point onto line
            float t = Vector2.Dot(toPoint, line) / (lineLength * lineLength);
            t = MathHelper.Clamp(t, 0f, 1f);
            
            Vector2 projection = lineStart + t * line;
            return Vector2.Distance(point, projection);
        }
        
        private bool DoLinesIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            // Find the four orientations needed for general and special cases
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);
            
            // General case
            if (o1 != o2 && o3 != o4)
                return true;
                
            // Special cases
            // p1, q1 and p2 are collinear and p2 lies on segment p1q1
            if (o1 == 0 && IsOnSegment(p1, p2, q1))
                return true;
                
            // p1, q1 and q2 are collinear and q2 lies on segment p1q1
            if (o2 == 0 && IsOnSegment(p1, q2, q1))
                return true;
                
            // p2, q2 and p1 are collinear and p1 lies on segment p2q2
            if (o3 == 0 && IsOnSegment(p2, p1, q2))
                return true;
                
            // p2, q2 and q1 are collinear and q1 lies on segment p2q2
            if (o4 == 0 && IsOnSegment(p2, q1, q2))
                return true;
                
            return false;
        }
        
        private int Orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            // Find orientation of ordered triplet (p, q, r)
            // Returns:
            // 0 -> p, q and r are collinear
            // 1 -> Clockwise orientation
            // 2 -> Counterclockwise orientation
            
            float val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            
            if (Math.Abs(val) < 0.001f) return 0;  // collinear
            return (val > 0) ? 1 : 2; // clockwise or counterclockwise
        }
        
        private bool IsOnSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            // Check if point q lies on line segment pr
            return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                   q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
        }

        private void TestWallCollisionAtMouse(Vector2 mousePos)
        {
            // Test Build engine collision by simulating movement from player position to mouse position
            if (_hasPlayerPosition)
            {
                Vector2 desiredMovement = mousePos - _playerPosition;
                Vector2 adjustedMovement = GetAdjustedMovement(_playerPosition, desiredMovement);
                Vector2 finalPos = _playerPosition + adjustedMovement;
                
                float originalDistance = desiredMovement.Length();
                float actualDistance = adjustedMovement.Length();
                
            }
        }


        private float CalculatePolygonArea(List<Vector2> vertices)
        {
            if (vertices.Count < 3) return 0;

            float area = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                area += vertices[i].X * vertices[j].Y;
                area -= vertices[j].X * vertices[i].Y;
            }

            return Math.Abs(area) / 2;
        }

        private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            if (polygon.Count < 3) return false;

            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];

                if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                    (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        private void DrawSelectionFeedback()
        {
            if (_currentEditMode == EditMode.Selection)
            {
                // Draw selected vertices with a different color
                foreach (var selectedVertex in _selectedVertices)
                {
                    DrawCircle(selectedVertex, 6, Color.Orange);
                }

                // Draw hover feedback for vertices in selection mode
                var hoveredVertex = GetVertexAt(_mouseWorldPosition, 8f);
                if (hoveredVertex.HasValue && !_selectedVertices.Contains(hoveredVertex.Value))
                {
                    DrawCircle(hoveredVertex.Value, 5, Color.LightBlue);
                }
            }
            else if (_currentEditMode == EditMode.Delete)
            {
                // Draw hover feedback for vertices in delete mode
                var hoveredVertex = GetVertexAt(_mouseWorldPosition, 8f);
                if (hoveredVertex.HasValue)
                {
                    DrawCircle(hoveredVertex.Value, 7, Color.Red);
                }
            }

            // Draw selected sector highlight
            if (_selectedSector != null && _currentEditMode == EditMode.Selection)
            {
                foreach (var wall in _selectedSector.Walls)
                {
                    DrawLine(wall.Start, wall.End, Color.Lime, 3);
                }
            }

            // Draw selected wall highlight
            if (_selectedWall != null && _currentEditMode == EditMode.Selection)
            {
                DrawLine(_selectedWall.Start, _selectedWall.End, Color.Yellow, 4);
            }

            // Draw player position marker
            if (_hasPlayerPosition)
            {
                // Draw selection indicator if player is selected
                if (_playerSelected)
                {
                    DrawCircle(_playerPosition, 12f, Color.Yellow);
                }
                
                DrawCircle(_playerPosition, 8f, Color.Red);
                DrawCircle(_playerPosition, 6f, Color.White);
                // Draw direction arrow pointing up (north)
                Vector2 arrowStart = _playerPosition;
                Vector2 arrowEnd = _playerPosition + new Vector2(0, -12);
                DrawLine(arrowStart, arrowEnd, Color.Red, 2);
                // Arrow head
                DrawLine(arrowEnd, arrowEnd + new Vector2(-3, 3), Color.Red, 2);
                DrawLine(arrowEnd, arrowEnd + new Vector2(3, 3), Color.Red, 2);
            }
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            var distance = Vector2.Distance(start, end);
            var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

            _spriteBatch.Draw(_pixelTexture, start, null, color, angle, Vector2.Zero,
                new Vector2(distance, thickness), SpriteEffects.None, 0);
        }

        private void Handle3DCameraInput(KeyboardState keyboardState, MouseState mouseState)
        {
            var speed = 100f;
            var deltaTime = 1f / 60f; // Approximate delta time

            // Calculate forward, right, and up vectors based on camera orientation
            var forward = new Vector3(
                (float)(Math.Cos(_camera3DPitch) * Math.Sin(_camera3DYaw)),
                (float)Math.Sin(_camera3DPitch),
                (float)(Math.Cos(_camera3DPitch) * Math.Cos(_camera3DYaw))
            );
            var right = Vector3.Cross(forward, Vector3.Up);
            var up = Vector3.Cross(right, forward);

            // WASD movement relative to camera orientation
            Vector3 movement = Vector3.Zero;
            if (keyboardState.IsKeyDown(Keys.W))
                movement += forward * speed * deltaTime;
            if (keyboardState.IsKeyDown(Keys.S))
                movement -= forward * speed * deltaTime;
            if (keyboardState.IsKeyDown(Keys.A))
                movement -= right * speed * deltaTime;
            if (keyboardState.IsKeyDown(Keys.D))
                movement += right * speed * deltaTime;

            // Q/E vertical movement (fly up/down freely)
            if (keyboardState.IsKeyDown(Keys.Q))
                movement += Vector3.Up * speed * deltaTime;
            if (keyboardState.IsKeyDown(Keys.E))
                movement -= Vector3.Up * speed * deltaTime;

            // Apply movement with optional collision detection
            if (_collisionMode)
            {
                // Calculate new 3D position
                var newCameraPos = _camera3DPosition + movement;
                var newPlayerPos = new Vector2(newCameraPos.X, -newCameraPos.Z);

                // Use current camera position as fallback if no player position is set
                var currentPos = _hasPlayerPosition ? _playerPosition : new Vector2(_camera3DPosition.X, -_camera3DPosition.Z);

                // Perform 3D collision detection
                var collisionResult = PerformCollisionDetection3D(currentPos, newPlayerPos, _camera3DPosition.Y, newCameraPos.Y);

                // Update positions with collision adjustments
                if (_hasPlayerPosition)
                    _playerPosition = new Vector2(collisionResult.X, collisionResult.Y);
                _camera3DPosition = new Vector3(collisionResult.X, collisionResult.Z, -collisionResult.Y);
            }
            else
            {
                // Apply movement normally without collision
                _camera3DPosition += movement;

                // Update player position in 2D based on horizontal movement (X and Z)
                if (_hasPlayerPosition && (movement.X != 0 || movement.Z != 0))
                {
                    _playerPosition.X += movement.X;
                    _playerPosition.Y += -movement.Z; // Convert 3D Z to 2D Y (negated for coordinate system)
                }
            }

            // Simple mouse look - cursor always follows mouse exactly
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                var mouseDelta = mouseState.Position.ToVector2() - _previousMouseState.Position.ToVector2();
                _camera3DYaw -= mouseDelta.X * 0.01f;
                _camera3DPitch -= mouseDelta.Y * 0.01f;
                _camera3DPitch =
                    MathHelper.Clamp(_camera3DPitch, -MathHelper.PiOver2 + 0.1f, MathHelper.PiOver2 - 0.1f);
            }

            // Update camera target based on yaw and pitch
            _camera3DTarget = _camera3DPosition + forward;
        }

        // Build Engine-style 3D collision detection
        private Vector3 PerformCollisionDetection3D(Vector2 oldPos2D, Vector2 newPos2D, float oldHeight, float newHeight)
        {
            // Start with desired position
            Vector3 resultPos = new Vector3(newPos2D.X, newPos2D.Y, newHeight);

            // Find current and target sectors BEFORE any collision processing
            var currentSector = FindMostSpecificSector(oldPos2D);
            var intendedTargetSector = FindMostSpecificSector(newPos2D);

            // Only debug when there's actual movement
            float movementDistance = Vector2.Distance(oldPos2D, newPos2D);
            if (movementDistance > 0.01f) // Only show debug for meaningful movement
            {
                Console.WriteLine($"MOVEMENT DEBUG: From ({oldPos2D.X:F1},{oldPos2D.Y:F1}) sector {currentSector?.Id} to ({newPos2D.X:F1},{newPos2D.Y:F1}) sector {intendedTargetSector?.Id}");
                Console.WriteLine($"MOVEMENT DISTANCE: {movementDistance:F3} units");

                // Debug: List all sectors and their point containment
                if (currentSector == intendedTargetSector)
                {
                    Console.WriteLine($"DEBUG: Both positions detected as same sector. Checking all sectors:");
                    foreach (var sector in _sectors)
                    {
                        bool containsOld = IsPointInSector(oldPos2D, sector);
                        bool containsNew = IsPointInSector(newPos2D, sector);
                        Console.WriteLine($"  Sector {sector.Id} (Type={sector.SectorType}, IsNested={sector.IsNested}): Old={containsOld}, New={containsNew}");
                    }
                }
            }

            // Check height-based sector transition FIRST, before wall collision
            if (currentSector != null && intendedTargetSector != null && currentSector != intendedTargetSector)
            {
                float currentFloorHeight = GetFloorHeight(oldPos2D, currentSector);
                float targetFloorHeight = GetFloorHeight(newPos2D, intendedTargetSector);
                float floorHeightDifference = Math.Abs(targetFloorHeight - currentFloorHeight);
                const float maxStepHeight = 24f;

                Console.WriteLine($"SECTOR TRANSITION: {currentSector.Id}→{intendedTargetSector.Id}, Heights: {currentFloorHeight}→{targetFloorHeight}, Diff: {floorHeightDifference}");

                if (floorHeightDifference > maxStepHeight)
                {
                    Console.WriteLine($"BLOCKING MOVEMENT: Height difference {floorHeightDifference} > {maxStepHeight}");
                    // Block the movement entirely - return to original position
                    return new Vector3(oldPos2D.X, oldPos2D.Y, oldHeight);
                }
                else
                {
                    Console.WriteLine($"ALLOWING STEP: Height difference {floorHeightDifference} <= {maxStepHeight}");
                }
            }

            // If no sectors found, allow free movement
            if (currentSector == null && intendedTargetSector == null)
                return resultPos;

            // Do horizontal collision (walls) after height check
            Vector2 horizontalPos = new Vector2(resultPos.X, resultPos.Y);
            
            // Only check walls from sectors that might be relevant
            HashSet<Sector> sectorsToCheck = new HashSet<Sector>();
            if (currentSector != null) sectorsToCheck.Add(currentSector);
            if (intendedTargetSector != null) sectorsToCheck.Add(intendedTargetSector);

            // ALWAYS check independent sectors for collision, regardless of player position
            // This fixes collision for independent sectors inside other sectors
            foreach (var sector in _sectors)
            {
                if (sector.SectorType == SectorType.Independent)
                {
                    sectorsToCheck.Add(sector);
                }
            }
            
            // Build Engine style: Only check solid walls for collision, not portal walls
            foreach (var sector in sectorsToCheck)
            {
                foreach (var wall in sector.Walls)
                {
                    // SKIP portal walls (two-sided walls that connect sectors) - they should have no collision
                    // All sector transitions are handled by the height check above
                    if (wall.IsTwoSided && wall.AdjacentSectorId.HasValue)
                        continue;

                    // For nested sectors, check height difference before applying wall collision
                    if (sector.IsNested)
                    {
                        // Check if height difference allows stepping
                        float currentFloorHeight = GetFloorHeight(oldPos2D, currentSector ?? sector);
                        float nestedFloorHeight = GetFloorHeight(horizontalPos, sector);
                        float heightDifference = Math.Abs(nestedFloorHeight - currentFloorHeight);

                        if (heightDifference <= 24f) // Allow stepping
                        {
                            Console.WriteLine($"NESTED STEP: Height diff {heightDifference} <= 24, allowing step into sector {sector.Id}");
                            continue; // Skip wall collision for this nested sector
                        }
                        else
                        {
                            Console.WriteLine($"NESTED WALL: Height diff {heightDifference} > 24, blocking step into sector {sector.Id}");
                        }
                    }

                    // Only check solid walls for collision (non-portal walls)
                    if (DoesWallBlockAtHeight(wall, oldPos2D, horizontalPos, resultPos.Z, currentSector))
                    {
                        var pushResult = PushAwayFromWall(horizontalPos, wall, PLAYER_RADIUS);
                        horizontalPos = pushResult;
                    }
                }
            }
            
            resultPos.X = horizontalPos.X;
            resultPos.Y = horizontalPos.Y;

            // Find the final sector after wall collision (for vertical collision)
            var finalSector = FindMostSpecificSector(new Vector2(resultPos.X, resultPos.Y)) ?? currentSector;
            
            // Do vertical collision (floor/ceiling) using the final determined sector
            if (finalSector != null)
            {
                float floorHeight = GetFloorHeight(new Vector2(resultPos.X, resultPos.Y), finalSector);
                float ceilingHeight = GetCeilingHeight(new Vector2(resultPos.X, resultPos.Y), finalSector);
                
                // Enforce floor/ceiling boundaries properly
                const float minFloorClearance = 8f; // Camera must be at least 8 units above floor
                const float minCeilingClearance = 1f; // Camera must be at least 1 unit below ceiling
                
                // Check if we need to step up or if it's too high (wall collision)
                float minHeight = floorHeight + minFloorClearance;
                if (resultPos.Z < minHeight)
                {
                    float heightDifference = minHeight - resultPos.Z;

                    Console.WriteLine($"Collision check: Sector {finalSector.Id}, Height diff: {heightDifference:F1}, IsLift: {finalSector.IsLift}, IsNested: {finalSector.IsNested}");

                    // For lift sectors, only move player up if they were standing on the lift when it started moving
                    if (finalSector.IsLift && !finalSector.PlayerWasStandingOnLift)
                    {
                        // Player wasn't on lift when it started - treat moving floor as wall (block horizontal movement)
                        resultPos.X = oldPos2D.X;
                        resultPos.Y = oldPos2D.Y;
                        finalSector = currentSector; // Stay in original sector
                        Console.WriteLine($"RESULT: Lift wall collision - Player wasn't standing on lift (Sector {finalSector.Id}, IsNested={finalSector.IsNested}) when it moved ({heightDifference} units), blocking horizontal movement");
                    }
                    else if (ShouldBlockMovementAsWall(finalSector, heightDifference))
                    {
                        // Too high to step - block horizontal movement entirely (treat as solid wall)
                        resultPos.X = oldPos2D.X;
                        resultPos.Y = oldPos2D.Y;
                        finalSector = currentSector; // Stay in original sector
                        Console.WriteLine($"RESULT: Wall collision - Height difference too large ({heightDifference} units, blocking horizontal movement)");
                    }
                    else
                    {
                        // Small enough to step up, or player was on lift when it started moving
                        resultPos.Z = minHeight;
                        Console.WriteLine($"RESULT: Step up - Setting height to {resultPos.Z} (floor: {floorHeight}, step height: {heightDifference})");
                    }
                }
                    
                // Don't allow going above ceiling - clearance
                float maxHeight = ceilingHeight - minCeilingClearance;
                if (resultPos.Z > maxHeight)
                {
                    resultPos.Z = maxHeight;
                    Console.WriteLine($"Ceiling collision: Setting height to {resultPos.Z} (ceiling: {ceilingHeight})");
                }
            }
            else
            {
                Console.WriteLine("No sector found for collision - allowing free movement");
            }
            
            // Check sprite collision
            if (currentSector != null)
            {
                foreach (var sprite in currentSector.Sprites)
                {
                    if (IsBlocking(sprite))
                    {
                        var pushResult = PushAwayFromSprite(new Vector2(resultPos.X, resultPos.Y), sprite, PLAYER_RADIUS);
                        resultPos.X = pushResult.X;
                        resultPos.Y = pushResult.Y;
                    }
                }
            }
            
            return resultPos;
        }
        
        private Vector2 PushAwayFromWall(Vector2 playerPos, Wall wall, float radius)
        {
            // Calculate distance from point to line segment
            float distance = PointToLineDistance(playerPos, wall.Start, wall.End);
            
            // If too close to wall, push away
            if (distance < radius)
            {
                // Calculate normal vector pointing away from wall
                Vector2 wallVector = wall.End - wall.Start;
                Vector2 wallNormal = new Vector2(-wallVector.Y, wallVector.X);
                wallNormal.Normalize();
                
                // Determine which side of the wall we're on
                Vector2 toPlayer = playerPos - wall.Start;
                if (Vector2.Dot(toPlayer, wallNormal) < 0)
                    wallNormal = -wallNormal;
                
                // Push player away from wall
                float pushDistance = radius - distance + 0.1f; // Small buffer
                return playerPos + wallNormal * pushDistance;
            }
            
            return playerPos;
        }
        
        private Vector2 PushAwayFromSprite(Vector2 playerPos, Sprite sprite, float playerRadius)
        {
            Vector2 spritePos = sprite.Position;
            float spriteRadius = 8f; // Default sprite radius
            
            float distance = Vector2.Distance(playerPos, spritePos);
            float combinedRadius = playerRadius + spriteRadius;
            
            if (distance < combinedRadius && distance > 0)
            {
                // Push away from sprite
                Vector2 direction = (playerPos - spritePos) / distance;
                float pushDistance = combinedRadius - distance + 0.1f;
                return playerPos + direction * pushDistance;
            }
            
            return playerPos;
        }
        
        private float PointToLineDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 line = lineEnd - lineStart;
            Vector2 toPoint = point - lineStart;
            
            float lineLength = line.Length();
            if (lineLength == 0) return Vector2.Distance(point, lineStart);
            
            // Project point onto line
            float t = Vector2.Dot(toPoint, line) / (lineLength * lineLength);
            t = MathHelper.Clamp(t, 0f, 1f);
            
            Vector2 projection = lineStart + t * line;
            return Vector2.Distance(point, projection);
        }
        
        private bool CanPassThroughWall(Wall wall, Vector2 oldPos, Vector2 newPos)
        {
            if (!wall.IsTwoSided || wall.AdjacentSectorId == null)
                return false;
                
            // Find the adjacent sector
            var adjacentSector = _sectors.FirstOrDefault(s => s.Id == wall.AdjacentSectorId.Value);
            if (adjacentSector == null) return false;
            
            // Get current sector
            var currentSector = FindSectorContainingPoint(oldPos);
            if (currentSector == null) return true; // No current sector, allow movement
            
            // Check height differences - can't pass if step is too high
            float stepHeight = adjacentSector.FloorHeight - currentSector.FloorHeight;
            float ceilingGap = adjacentSector.CeilingHeight - adjacentSector.FloorHeight;
            
            // Can't pass if step up is too high (> 24 units) or ceiling too low (< 56 units)
            if (stepHeight > 24f || ceilingGap < 56f)
                return false;
                
            return true;
        }
        
        private bool IsBlocking(Sprite sprite)
        {
            // Most sprites are blocking except pickups and decorations
            return sprite.LoTag != TagConstants.SPRITE_PICKUP && sprite.LoTag != TagConstants.SPRITE_DECORATION;
        }
        
        // Build engine authentic slope height calculation - getzsofslope equivalent
        private void GetZsOfSlope(Sector sector, float x, float y, out float ceilz, out float florz)
        {
            // Check if floor/ceiling are sloped (equivalent to Build engine stat & 1)
            bool floorSloped = sector.HasSlopes && sector.VertexHeights.Count > 0;
            bool ceilingSloped = sector.HasSlopes && sector.VertexHeights.Count > 0;

            if (floorSloped)
            {
                // Calculate floor height using Build engine slope math
                florz = CalculateSlopeZ(sector, x, y, true); // true = floor
            }
            else
            {
                // Flat floor
                florz = sector.FloorHeight;
            }

            if (ceilingSloped)
            {
                // Calculate ceiling height using Build engine slope math
                ceilz = CalculateSlopeZ(sector, x, y, false); // false = ceiling
            }
            else
            {
                // Flat ceiling
                ceilz = sector.CeilingHeight;
            }

            // Apply animation offset for any sector that has an offset (including lifts at rest position)
            if (Math.Abs(sector.AnimationHeightOffset) > 0.01f)
            {
                florz += sector.AnimationHeightOffset;

                // For lifts, only move the floor (Duke Nukem style)
                // For other animations (sliding doors), move both floor and ceiling
                if (!sector.IsLift)
                {
                    ceilz += sector.AnimationHeightOffset;
                }
            }

            // Console.WriteLine($"BUILD SLOPE: Sector {sector.Id} at ({x:F1},{y:F1}) -> Floor={florz:F1}, Ceiling={ceilz:F1} (sloped: floor={floorSloped}, ceiling={ceilingSloped}, animated: {sector.IsLift && sector.IsAnimating})");
        }
        
        // Calculate slope height using Build engine method
        private float CalculateSlopeZ(Sector sector, float x, float y, bool isFloor)
        {
            if (sector.VertexHeights.Count < 3)
            {
                // Auto-generate vertex heights if not available
                sector.GenerateSlopeVertexHeights(0f, 32f, 64f, 96f);
            }
            
            // Build engine slope calculation using vertex data - use direct calculation to avoid circular dependency
            // This mimics the authentic Build engine getflorzofslope/getceilzofslope functions
            return CalculateLegacyInterpolation(new Vector2(x, y), sector, isFloor);
        }
        
        // Legacy interpolation calculation (avoids circular dependency with new slope plane system)
        private float CalculateLegacyInterpolation(Vector2 position, Sector sector, bool isFloor)
        {
            if (sector.Vertices.Count < 3 || sector.VertexHeights.Count < 3)
                return isFloor ? sector.FloorHeight : sector.CeilingHeight;
            
            // Find the correct triangle that contains this position
            var triangleIndices = FindTriangleContainingPoint(position, sector);
            
            if (triangleIndices == null)
            {
                // Fallback: use closest triangle if point is outside polygon
                triangleIndices = FindClosestTriangle(position, sector);
            }
            
            if (triangleIndices == null)
            {
                // Ultimate fallback: use first 3 vertices
                triangleIndices = new[] { 0, 1, 2 };
            }
            
            // Get the vertices for this triangle
            var v1 = sector.Vertices[triangleIndices[0]];
            var v2 = sector.Vertices[triangleIndices[1]];
            var v3 = sector.Vertices[triangleIndices[2]];
            
            var vh1 = sector.VertexHeights.FirstOrDefault(vh => vh.VertexIndex == triangleIndices[0]);
            var vh2 = sector.VertexHeights.FirstOrDefault(vh => vh.VertexIndex == triangleIndices[1]);
            var vh3 = sector.VertexHeights.FirstOrDefault(vh => vh.VertexIndex == triangleIndices[2]);
            
            if (vh1 == null || vh2 == null || vh3 == null)
                return isFloor ? sector.FloorHeight : sector.CeilingHeight;
            
            float z1 = isFloor ? vh1.FloorHeight : vh1.CeilingHeight;
            float z2 = isFloor ? vh2.FloorHeight : vh2.CeilingHeight;
            float z3 = isFloor ? vh3.FloorHeight : vh3.CeilingHeight;
            
            // Calculate plane equation coefficients using cross product
            Vector3 p1 = new Vector3(v1.X, v1.Y, z1);
            Vector3 p2 = new Vector3(v2.X, v2.Y, z2);
            Vector3 p3 = new Vector3(v3.X, v3.Y, z3);
            
            Vector3 normal = Vector3.Cross(p2 - p1, p3 - p1);
            
            // Avoid division by zero
            if (Math.Abs(normal.Z) < 0.0001f)
                return z1; // Fallback to first vertex height
            
            // Calculate z at position using plane equation
            float interpolatedHeight = p1.Z - (normal.X * (position.X - p1.X) + normal.Y * (position.Y - p1.Y)) / normal.Z;
            
            return interpolatedHeight;
        }
        
        // Find which triangle in the polygon contains the given point
        private int[]? FindTriangleContainingPoint(Vector2 point, Sector sector)
        {
            if (sector.Vertices.Count < 3) return null;
            
            // Simple triangulation approach: use fan triangulation from first vertex
            for (int i = 1; i < sector.Vertices.Count - 1; i++)
            {
                var triangle = new[] { 0, i, i + 1 };
                
                var v1 = sector.Vertices[triangle[0]];
                var v2 = sector.Vertices[triangle[1]];
                var v3 = sector.Vertices[triangle[2]];
                
                if (IsPointInTriangle(point, v1, v2, v3))
                {
                    return triangle;
                }
            }
            
            return null;
        }
        
        // Find the closest triangle to the point (fallback when point is outside polygon)
        private int[]? FindClosestTriangle(Vector2 point, Sector sector)
        {
            if (sector.Vertices.Count < 3) return null;
            
            float closestDistance = float.MaxValue;
            int[]? closestTriangle = null;
            
            // Check all triangles in fan triangulation
            for (int i = 1; i < sector.Vertices.Count - 1; i++)
            {
                var triangle = new[] { 0, i, i + 1 };
                
                var v1 = sector.Vertices[triangle[0]];
                var v2 = sector.Vertices[triangle[1]];
                var v3 = sector.Vertices[triangle[2]];
                
                // Calculate distance to triangle centroid
                var centroid = new Vector2((v1.X + v2.X + v3.X) / 3, (v1.Y + v2.Y + v3.Y) / 3);
                float distance = Vector2.Distance(point, centroid);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTriangle = triangle;
                }
            }
            
            return closestTriangle;
        }
        
        // Check if a point is inside a triangle using barycentric coordinates
        private bool IsPointInTriangle(Vector2 point, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            // Calculate vectors
            var v0 = v3 - v1;
            var v1v = v2 - v1;
            var v2v = point - v1;
            
            // Calculate dot products
            var dot00 = Vector2.Dot(v0, v0);
            var dot01 = Vector2.Dot(v0, v1v);
            var dot02 = Vector2.Dot(v0, v2v);
            var dot11 = Vector2.Dot(v1v, v1v);
            var dot12 = Vector2.Dot(v1v, v2v);
            
            // Calculate barycentric coordinates
            var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            var v = (dot00 * dot12 - dot01 * dot02) * invDenom;
            
            // Check if point is in triangle
            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }
        
        // Get floor height at specific position - Build engine style
        private float GetFloorHeight(Vector2 position, Sector sector)
        {
            GetZsOfSlope(sector, position.X, position.Y, out float ceilz, out float florz);
            return florz;
        }
        
        // Get ceiling height at specific position - Build engine style
        private float GetCeilingHeight(Vector2 position, Sector sector)
        {
            GetZsOfSlope(sector, position.X, position.Y, out float ceilz, out float florz);
            return ceilz;
        }

        // Check if player is currently standing on the given sector
        private bool IsPlayerStandingOnSector(Sector sector)
        {
            if (!_hasPlayerPosition) return false;

            Vector2 playerPos = _playerPosition;

            // Check if player is within this sector
            if (!IsPointInSector(playerPos, sector)) return false;

            // Check if player height is close to the sector floor
            float sectorFloorHeight = GetFloorHeight(playerPos, sector);
            float playerHeight = _camera3DPosition.Y;

            // Player is standing on sector if they're within a reasonable distance above the floor
            const float standingThreshold = 12f; // Within 12 units of floor = standing on it
            return Math.Abs(playerHeight - sectorFloorHeight) <= standingThreshold;
        }




        // Check if player is getting too close to a wall collision boundary and apply gentle resistance
        private Vector2 CheckWallCollisionProximity(Vector2 position, Sector currentSector)
        {
            const float warningDistance = 4f; // Distance from boundary to start applying resistance
            Vector2 adjustedPosition = position;

            // Check all nested sectors for boundary proximity
            foreach (var nestedSector in _sectors.Where(s => s.IsNested))
            {
                float distanceToBoundary = GetDistanceToSectorBoundary(position, nestedSector);

                if (distanceToBoundary <= warningDistance)
                {
                    bool isInsideNested = IsPointInSector(position, nestedSector);

                    if (!isInsideNested && currentSector != null)
                    {
                        // Approaching nested sector from outside
                        float currentFloorHeight = GetFloorHeight(position, currentSector);
                        float nestedFloorHeight = GetFloorHeight(position, nestedSector);
                        float heightDifference = Math.Abs(nestedFloorHeight - currentFloorHeight);

                        if (ShouldBlockMovementAsWall(currentSector, heightDifference))
                        {
                            // Push away from the nested sector boundary
                            Vector2 nestedCenter = Vector2.Zero;
                            foreach (var vertex in nestedSector.Vertices)
                                nestedCenter += vertex;
                            nestedCenter /= nestedSector.Vertices.Count;

                            Vector2 pushDirection = Vector2.Normalize(position - nestedCenter);
                            float pushStrength = (warningDistance - distanceToBoundary) / warningDistance;
                            adjustedPosition += pushDirection * pushStrength * 2f; // Stronger push away from nested sector
                        }
                    }
                    else if (isInsideNested && currentSector == nestedSector)
                    {
                        // Inside nested sector trying to exit
                        var parentSector = _sectors.FirstOrDefault(s => !s.IsNested && IsPointInSector(position, s));
                        if (parentSector != null)
                        {
                            float nestedFloorHeight = GetFloorHeight(position, nestedSector);
                            float parentFloorHeight = GetFloorHeight(position, parentSector);
                            float heightDifference = Math.Abs(parentFloorHeight - nestedFloorHeight);

                            if (ShouldBlockMovementAsWall(nestedSector, heightDifference))
                            {
                                // Push back toward center of nested sector (keep in pit)
                                Vector2 nestedCenter = Vector2.Zero;
                                foreach (var vertex in nestedSector.Vertices)
                                    nestedCenter += vertex;
                                nestedCenter /= nestedSector.Vertices.Count;

                                Vector2 pushDirection = Vector2.Normalize(nestedCenter - position);
                                float pushStrength = (warningDistance - distanceToBoundary) / warningDistance;
                                adjustedPosition += pushDirection * pushStrength * 2f; // Stronger push toward center
                            }
                        }
                    }
                }
            }

            return adjustedPosition;
        }

        // Check if a point is near a sector boundary within given distance
        private bool IsPointNearSector(Vector2 point, Sector sector, float distance)
        {
            return GetDistanceToSectorBoundary(point, sector) <= distance;
        }

        // Get distance from point to closest sector boundary
        private float GetDistanceToSectorBoundary(Vector2 point, Sector sector)
        {
            float minDistance = float.MaxValue;

            for (int i = 0; i < sector.Vertices.Count; i++)
            {
                Vector2 wallStart = sector.Vertices[i];
                Vector2 wallEnd = sector.Vertices[(i + 1) % sector.Vertices.Count];

                float distanceToWall = DistancePointToLine(point, wallStart, wallEnd);
                minDistance = Math.Min(minDistance, distanceToWall);
            }

            return minDistance;
        }

        // Get direction to push away from sector boundary
        private Vector2 GetPushDirectionFromSector(Vector2 point, Sector sector)
        {
            Vector2 sectorCenter = Vector2.Zero;
            foreach (var vertex in sector.Vertices)
                sectorCenter += vertex;
            sectorCenter /= sector.Vertices.Count;

            Vector2 direction = point - sectorCenter;
            return direction.Length() > 0 ? Vector2.Normalize(direction) : Vector2.UnitX;
        }

        // Distance from point to line segment
        private float DistancePointToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 line = lineEnd - lineStart;
            float lineLength = line.Length();
            if (lineLength == 0) return Vector2.Distance(point, lineStart);

            Vector2 lineDirection = line / lineLength;
            Vector2 pointToStart = point - lineStart;
            float projection = Vector2.Dot(pointToStart, lineDirection);
            projection = Math.Clamp(projection, 0, lineLength);

            Vector2 closestPoint = lineStart + lineDirection * projection;
            return Vector2.Distance(point, closestPoint);
        }

        // Determine if movement should be blocked as wall collision vs allowed as step
        // <24 units = step, >=24 units = wall collision
        private bool ShouldBlockMovementAsWall(Sector currentSector, float heightDifference)
        {
            const float stepThreshold = 24f; // Units below this are steps, above are walls

            Console.WriteLine($"Step/Wall check: Sector {currentSector.Id}, IsNested={currentSector.IsNested}, IsLift={currentSector.IsLift}, HeightDiff={heightDifference:F1}");

            // Apply step/wall logic to nested sectors and lift sectors
            if (!currentSector.IsNested && !currentSector.IsLift)
            {
                Console.WriteLine("Not nested/lift sector - allowing step up");
                return false;
            }

            // If height difference is above threshold, treat as wall collision
            bool isWall = heightDifference >= stepThreshold;
            Console.WriteLine($"Nested/Lift sector - Height {heightDifference:F1} vs threshold {stepThreshold} = {(isWall ? "WALL" : "STEP")}");
            return isWall;
        }
        
        // Interpolate height using proper Build engine slope plane calculation
        // Cache for slope plane calculations to improve performance
        private Dictionary<(int sectorId, bool isFloor), SlopePlane?> _slopePlaneCache = new Dictionary<(int, bool), SlopePlane?>();
        
        // Configurable texture scaling system
        public static class TextureScaleSettings
        {
            // Global default texture scaling (Build Engine style: 64 units per texture repeat)
            public static float DefaultWallTextureScaleX { get; set; } = 64f;
            public static float DefaultWallTextureScaleY { get; set; } = 64f;
            public static float DefaultFloorTextureScale { get; set; } = 64f;
            public static float DefaultCeilingTextureScale { get; set; } = 64f;
            
            // Per-surface type scaling factors
            public static Dictionary<string, float> TextureScales { get; set; } = new Dictionary<string, float>
            {
                { "wall_default", 64f },
                { "floor_default", 64f },
                { "ceiling_default", 64f },
                { "slope_default", 64f }
            };
            
            // Texture-specific scaling overrides (texture name -> scale factor)
            public static Dictionary<string, float> TextureSpecificScales { get; set; } = new Dictionary<string, float>();
            
            // Methods to get appropriate scaling for different surface types
            public static float GetWallTextureScale(string textureName = "", string surfaceType = "wall")
            {
                // Check texture-specific override first
                if (!string.IsNullOrEmpty(textureName) && TextureSpecificScales.ContainsKey(textureName))
                    return TextureSpecificScales[textureName];
                
                // Check surface type specific
                if (TextureScales.ContainsKey($"{surfaceType}_default"))
                    return TextureScales[$"{surfaceType}_default"];
                
                return DefaultWallTextureScaleX;
            }
            
            public static Vector2 GetWallTextureScaleVector(string textureName = "")
            {
                float scale = GetWallTextureScale(textureName, "wall");
                return new Vector2(scale, scale); // Can be made non-uniform if needed
            }
            
            public static float GetFloorCeilingTextureScale(string textureName = "", bool isFloor = true)
            {
                // Check texture-specific override first
                if (!string.IsNullOrEmpty(textureName) && TextureSpecificScales.ContainsKey(textureName))
                    return TextureSpecificScales[textureName];
                
                string surfaceType = isFloor ? "floor" : "ceiling";
                if (TextureScales.ContainsKey($"{surfaceType}_default"))
                    return TextureScales[$"{surfaceType}_default"];
                
                return isFloor ? DefaultFloorTextureScale : DefaultCeilingTextureScale;
            }
        }
        
        private float InterpolateHeight(Vector2 position, Sector sector, bool isFloor)
        {
            // Use modern SlopePlane system if available, fall back to legacy calculation
            var slopePlane = GetOrCreateSlopePlane(sector, isFloor);
            
            if (slopePlane != null)
            {
                return slopePlane.GetZAt(position.X, position.Y);
            }
            
            // Fallback to base height if no slope data
            return isFloor ? sector.FloorHeight : sector.CeilingHeight;
        }
        
        private SlopePlane? GetOrCreateSlopePlane(Sector sector, bool isFloor)
        {
            // Check cache first for performance
            var cacheKey = (sector.Id, isFloor);
            if (_slopePlaneCache.TryGetValue(cacheKey, out var cachedPlane))
            {
                return cachedPlane;
            }
            
            // Use existing modern slope plane if available
            var existingPlane = isFloor ? sector.FloorPlane : sector.CeilingPlane;
            if (existingPlane != null)
            {
                _slopePlaneCache[cacheKey] = existingPlane;
                return existingPlane;
            }
            
            // Generate slope plane from legacy vertex height data for collision only
            var generatedPlane = GenerateSlopePlaneFromVertexHeights(sector, isFloor);
            _slopePlaneCache[cacheKey] = generatedPlane;
            
            // DO NOT UPDATE SECTOR PLANES - collision detection shouldn't modify rendering geometry
            // The sector's FloorPlane/CeilingPlane should only be updated by explicit user actions
            // This prevents first-time collision mode from changing the visual geometry
            
            return generatedPlane;
        }
        
        private SlopePlane? GenerateSlopePlaneFromVertexHeights(Sector sector, bool isFloor)
        {
            if (sector.Vertices.Count < 3 || sector.VertexHeights.Count < 3)
                return null;
            
            // Collect all valid vertex height data with bounds checking
            var points = new List<Vector3>();
            
            for (int i = 0; i < sector.Vertices.Count; i++)
            {
                var vertex = sector.Vertices[i];
                var vertexHeight = sector.VertexHeights.FirstOrDefault(vh => vh.VertexIndex == i);
                
                if (vertexHeight != null)
                {
                    float height = isFloor ? vertexHeight.FloorHeight : vertexHeight.CeilingHeight;
                    points.Add(new Vector3(vertex.X, vertex.Y, height));
                }
            }
            
            if (points.Count < 3)
                return null;
            
            // Use least squares fitting for better stability with complex polygons
            return FitPlaneToPoints(points);
        }
        
        private SlopePlane? FitPlaneToPoints(List<Vector3> points)
        {
            if (points.Count < 3)
                return null;
            
            // For 3 points, use exact calculation
            if (points.Count == 3)
            {
                return CalculateExactPlane(points[0], points[1], points[2]);
            }
            
            // For more than 3 points, use least squares fitting for better stability
            return CalculateLeastSquaresPlane(points);
        }
        
        private SlopePlane? CalculateExactPlane(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // Calculate plane using cross product method
            Vector3 v1 = p2 - p1;
            Vector3 v2 = p3 - p1;
            Vector3 normal = Vector3.Cross(v1, v2);
            
            // Check for degenerate case (collinear points)
            if (Math.Abs(normal.Z) < 0.0001f)
                return null;
            
            // Convert to Build engine slope format: z(x,y) = baseZ + (x-refX)*dx + (y-refY)*dy
            float dx = -normal.X / normal.Z;
            float dy = -normal.Y / normal.Z;
            
            // Use first point as reference
            Vector2 refPoint = new Vector2(p1.X, p1.Y);
            float baseZ = p1.Z;
            
            return new SlopePlane(baseZ, refPoint, dx, dy);
        }
        
        private SlopePlane? CalculateLeastSquaresPlane(List<Vector3> points)
        {
            int n = points.Count;
            
            // Calculate centroid for numerical stability
            Vector3 centroid = Vector3.Zero;
            foreach (var point in points)
            {
                centroid += point;
            }
            centroid /= n;
            
            // Set up normal equation matrices for least squares: A^T * A * x = A^T * b
            // Where plane equation is: z = a*x + b*y + c (c is the constant term at centroid)
            double sumXX = 0, sumXY = 0, sumYY = 0;
            double sumXZ = 0, sumYZ = 0;
            
            foreach (var point in points)
            {
                double x = point.X - centroid.X;
                double y = point.Y - centroid.Y;
                double z = point.Z - centroid.Z;
                
                sumXX += x * x;
                sumXY += x * y;
                sumYY += y * y;
                sumXZ += x * z;
                sumYZ += y * z;
            }
            
            // Solve 2x2 system for dx and dy coefficients
            double determinant = sumXX * sumYY - sumXY * sumXY;
            
            if (Math.Abs(determinant) < 0.0001)
            {
                // Degenerate case - fall back to simple three-point calculation
                return CalculateExactPlane(points[0], points[1], points[2]);
            }
            
            double dx = (sumXZ * sumYY - sumYZ * sumXY) / determinant;
            double dy = (sumYZ * sumXX - sumXZ * sumXY) / determinant;
            
            // Reference point is the centroid for numerical stability
            Vector2 refPoint = new Vector2(centroid.X, centroid.Y);
            float baseZ = centroid.Z;
            
            return new SlopePlane(baseZ, refPoint, (float)dx, (float)dy);
        }
        
        // Check if wall blocks movement at specific height
        private bool DoesWallBlockAtHeight(Wall wall, Vector2 oldPos, Vector2 newPos, float playerHeight, Sector currentSector)
        {
            // Non-two-sided walls always block
            if (!wall.IsTwoSided)
                return true;
                
            // Find adjacent sector
            if (wall.AdjacentSectorId == null)
                return true;
                
            var adjacentSector = _sectors.FirstOrDefault(s => s.Id == wall.AdjacentSectorId.Value);
            if (adjacentSector == null)
                return true;
                
            // Get floor/ceiling heights for both sectors
            float adjFloor = GetFloorHeight(newPos, adjacentSector);
            float adjCeiling = GetCeilingHeight(newPos, adjacentSector);
            
            // If no current sector, check if we can enter adjacent sector
            if (currentSector == null)
            {
                return playerHeight < adjFloor || playerHeight + 4f > adjCeiling;
            }
            
            // Always check height differences - even for nested sectors
            float currentFloor = GetFloorHeight(oldPos, currentSector);
            
            // Check if player can step up/down between sectors
            float stepUpHeight = adjFloor - currentFloor;
            float stepDownHeight = currentFloor - adjFloor;
            
            // Pit wall collision - use smaller step height for nested sectors
            float maxStepHeight = currentSector.IsNested ? 5f : 24f; // 5 units for pits, 24 for normal sectors
            
            if (stepUpHeight > maxStepHeight)
            {
                Console.WriteLine($"Wall blocks: step up too high ({stepUpHeight} units, max {maxStepHeight}) - {(currentSector.IsNested ? "pit wall" : "normal wall")}");
                return true; // Wall blocks - can't step up this high
            }
            
            // Check if this is a nested sector transition
            bool isNestedTransition = IsNestedSectorTransition(currentSector, adjacentSector);
            if (isNestedTransition)
            {
                // For nested sectors, be more strict about step heights
                if (Math.Abs(stepUpHeight) > 16f || Math.Abs(stepDownHeight) > 16f)
                {
                    Console.WriteLine($"Nested sector wall blocks: height difference too large ({Math.Abs(stepUpHeight)} units)");
                    return true; // Block transition - pit/divot walls block
                }
            }
            
            // Check ceiling clearance
            float ceilingClearance = adjCeiling - adjFloor;
            if (ceilingClearance < 56f) // Need at least 56 units for player height
            {
                Console.WriteLine($"Wall blocks: ceiling too low ({ceilingClearance} units, need 56)");
                return true;
            }
            
            // Check if player height fits in adjacent sector
            if (playerHeight < adjFloor || playerHeight + 56f > adjCeiling)
            {
                Console.WriteLine($"Wall blocks: player doesn't fit in adjacent sector");
                return true;
            }
            
            return false; // Wall doesn't block - valid transition
        }
        
        // Find the most specific sector at a position (prioritizes nested sectors)
        private Sector FindMostSpecificSector(Vector2 position)
        {
            Sector mostSpecificSector = null;
            int maxNestingLevel = -1;
            
            foreach (var sector in _sectors)
            {
                if (IsPointInSector(position, sector))
                {
                    // Calculate nesting level (0 = root, higher = more nested)
                    int nestingLevel = GetNestingLevel(sector);
                    
                    if (nestingLevel > maxNestingLevel)
                    {
                        maxNestingLevel = nestingLevel;
                        mostSpecificSector = sector;
                    }
                }
            }
            
            if (mostSpecificSector != null)
            {
                // Console.WriteLine($"Found sector {mostSpecificSector.Id} at position {position} (nesting level: {maxNestingLevel})");
            }
            
            return mostSpecificSector;
        }
        
        // Calculate nesting level of a sector
        private int GetNestingLevel(Sector sector)
        {
            if (!sector.IsNested || sector.ParentSectorId == null)
                return 0;
                
            // Find parent and recurse
            var parent = _sectors.FirstOrDefault(s => s.Id == sector.ParentSectorId.Value);
            if (parent != null)
                return 1 + GetNestingLevel(parent);
                
            return 1; // Default if parent not found
        }
        
        // Check if wall is a nested sector portal
        private bool IsNestedSectorPortal(Wall wall, Sector currentSector, Sector adjacentSector)
        {
            // Check if one sector is nested within the other
            if (currentSector.IsNested && currentSector.ParentSectorId == adjacentSector.Id)
                return true; // Current sector is nested in adjacent sector
                
            if (adjacentSector.IsNested && adjacentSector.ParentSectorId == currentSector.Id)
                return true; // Adjacent sector is nested in current sector
                
            // Also check if both sectors are at the same nesting level with similar heights
            if (currentSector.IsNested && adjacentSector.IsNested && 
                currentSector.ParentSectorId == adjacentSector.ParentSectorId)
            {
                // Both nested in same parent - check if heights are similar (not a pit/divot)
                float floorDiff = Math.Abs(currentSector.FloorHeight - adjacentSector.FloorHeight);
                float ceilingDiff = Math.Abs(currentSector.CeilingHeight - adjacentSector.CeilingHeight);
                // Much stricter check - only very small differences count as portals
                return floorDiff < 2f && ceilingDiff < 2f;
            }
            
            // For non-nested sectors, be very strict about portal detection
            if (!currentSector.IsNested && !adjacentSector.IsNested)
            {
                float floorDiff = Math.Abs(currentSector.FloorHeight - adjacentSector.FloorHeight);
                float ceilingDiff = Math.Abs(currentSector.CeilingHeight - adjacentSector.CeilingHeight);
                // Only treat as portal if heights are nearly identical
                return floorDiff < 1f && ceilingDiff < 1f;
            }
            
            return false;
        }
        
        // Check if this is a transition between nested sectors (pit/divot walls)
        private bool IsNestedSectorTransition(Sector currentSector, Sector adjacentSector)
        {
            // Check if one sector is nested within the other
            if (currentSector.IsNested && currentSector.ParentSectorId == adjacentSector.Id)
                return true; // Moving from nested sector to its parent
                
            if (adjacentSector.IsNested && adjacentSector.ParentSectorId == currentSector.Id)
                return true; // Moving from parent to nested sector
                
            // Check if both are nested in the same parent (pit to pit transition)
            if (currentSector.IsNested && adjacentSector.IsNested && 
                currentSector.ParentSectorId == adjacentSector.ParentSectorId)
                return true;
                
            return false;
        }

        private void Draw3DViewport()
        {
            var viewportBounds = GetViewportBounds();
            var originalViewport = GraphicsDevice.Viewport;

            GraphicsDevice.Viewport = new Viewport(viewportBounds.X, viewportBounds.Y,
                viewportBounds.Width, viewportBounds.Height);

            GraphicsDevice.Clear(Color.DarkSlateGray);

            // Setup 3D projection
            var aspectRatio = (float)viewportBounds.Width / viewportBounds.Height;
            _basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, aspectRatio, 1f, 1000f);
            _basicEffect.View = Matrix.CreateLookAt(_camera3DPosition, _camera3DTarget, Vector3.Up);
            _basicEffect.World = Matrix.Identity;

            // Set wireframe mode and disable depth culling to ensure visibility
            var rasterizerState = new RasterizerState();
            if (_wireframeMode)
            {
                rasterizerState.FillMode = FillMode.WireFrame;
            }
            else
            {
                rasterizerState.FillMode = FillMode.Solid;
            }

            rasterizerState.CullMode = CullMode.None;
            GraphicsDevice.RasterizerState = rasterizerState;

            // Enable depth testing to ensure proper rendering order
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            Draw3DSectors();

            // Draw 3D cursor
            Draw3DCursor();

            GraphicsDevice.Viewport = originalViewport;
        }

        private void Draw3DSectors()
        {
            var renderedSectors = new HashSet<int>();

            // First, render all independent (parent) sectors
            foreach (var sector in _sectors.Where(s => !s.IsNested))
            {
                Draw3DSector(sector, renderedSectors, 0); // Depth 0 for main sectors
            }

            // Then, render nested sectors with slight height offset to ensure they render on top
            foreach (var sector in _sectors.Where(s => s.IsNested))
            {
                Draw3DNestedSector(sector, renderedSectors);
            }
        }

        private void Draw3DNestedSector(Sector sector, HashSet<int> renderedSectors)
        {
            if (renderedSectors.Contains(sector.Id))
                return;

            renderedSectors.Add(sector.Id);

            // Draw nested sector floor/ceiling with slight offset to render on top of parent
            Draw3DNestedFloorCeiling(sector);

            // Draw walls (these use the existing nested sector wall rendering)
            foreach (var wall in sector.Walls)
            {
                // Use actual wall texture color for nested sectors, just like independent sectors
                var wallColor = GetTextureColor(sector.WallTexture);

                // Draw nested sector walls as height transitions
                DrawNestedSectorHeightTransitionWall(wall, sector, sector.WallTexture);
            }

            // Draw sprites in this sector
            foreach (var sprite in sector.Sprites)
            {
                DrawSprite3D(sprite, sector);
            }

        }

        private void Draw3DSector(Sector sector, HashSet<int> renderedSectors, int depth)
        {
            if (renderedSectors.Contains(sector.Id) || depth > 5) // Prevent infinite recursion
                return;

            renderedSectors.Add(sector.Id);

            // Draw this sector's geometry
            Draw3DFloorCeiling(sector);

            // Draw sprites in this sector
            foreach (var sprite in sector.Sprites)
            {
                DrawSprite3D(sprite, sector);
            }

            var wallColor = GetTextureColor(sector.WallTexture);

            foreach (var wall in sector.Walls)
            {
                // Handle two-sided walls (shared edges between sectors)
                if (wall.IsTwoSided && wall.AdjacentSectorId.HasValue)
                {
                    var adjacentSector = _sectors.FirstOrDefault(s => s.Id == wall.AdjacentSectorId.Value);
                    if (adjacentSector != null)
                    {
                        // Draw only the upper/lower wall sections where heights differ
                        Draw3DTwoSidedWall(wall, sector, adjacentSector, sector.WallTexture);
                        
                        // Recursively draw the adjacent sector so it's visible through the opening
                        // Only draw if we haven't already rendered it (to prevent infinite recursion)
                        if (!renderedSectors.Contains(adjacentSector.Id))
                        {
                            Draw3DSector(adjacentSector, renderedSectors, depth + 1);
                        }
                        continue;
                    }
                }
                
                // For nested sectors, draw height transition walls instead of full walls
                if (sector.IsNested)
                {
                    DrawNestedSectorHeightTransitionWall(wall, sector, sector.WallTexture);
                    continue;
                }
                
                // Draw regular solid wall (with slope support)
                if (sector.HasSlopes)
                {
                    Draw3DSlopedWall(wall.Start, wall.End, sector, sector.WallTexture);
                }
                else
                {
                    Draw3DWall(wall.Start, wall.End, sector.FloorHeight, sector.CeilingHeight, sector.WallTexture);
                }
            }

        }

        private void Draw3DWall(Vector2 start, Vector2 end, float floorHeight, float ceilingHeight, string textureName)
        {
            var texture = GetTexture(textureName);
            if (texture == null) return;

            // Don't draw walls with no height difference
            if (Math.Abs(ceilingHeight - floorHeight) < 0.1f) return;

            // Ensure floor is always lower than ceiling for proper rendering
            float actualFloor = Math.Min(floorHeight, ceilingHeight);
            float actualCeiling = Math.Max(floorHeight, ceilingHeight);

            _basicEffect.Texture = texture;

            // Set sampler state to enable texture wrapping/tiling
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            var vertices = new VertexPositionTexture[4];

            // Calculate wall length for proper UV tiling
            float wallLength = Vector2.Distance(start, end);
            float wallHeight = actualCeiling - actualFloor;
            
            // Texture tiling: repeat texture based on world units (like Build engine)
            // Assume texture represents a certain world unit size (e.g., 64 units wide, 64 units tall)
            float textureWorldWidth = 64f;  // World units per texture repeat horizontally
            float textureWorldHeight = 64f; // World units per texture repeat vertically
            
            float uRepeat = wallLength / textureWorldWidth;
            float vRepeat = wallHeight / textureWorldHeight;

            // Convert 2D coordinates to 3D (Z becomes Y, Y becomes -Z for proper orientation)
            vertices[0] = new VertexPositionTexture(new Vector3(start.X, actualFloor, -start.Y), new Vector2(0, vRepeat)); // Bottom left
            vertices[1] = new VertexPositionTexture(new Vector3(start.X, actualCeiling, -start.Y), new Vector2(0, 0)); // Top left
            vertices[2] = new VertexPositionTexture(new Vector3(end.X, actualCeiling, -end.Y), new Vector2(uRepeat, 0)); // Top right
            vertices[3] = new VertexPositionTexture(new Vector3(end.X, actualFloor, -end.Y), new Vector2(uRepeat, vRepeat)); // Bottom right

            var indices = new short[] { 0, 1, 2, 0, 2, 3 };

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
            }
        }

        private void Draw3DTwoSidedWall(Wall wall, Sector sectorA, Sector sectorB, string wallTexture)
        {
            // For two-sided walls, only draw upper/lower textures where height differs
            // This follows Build Engine conventions

            float floorA = sectorA.FloorHeight;
            float ceilingA = sectorA.CeilingHeight;
            float floorB = sectorB.FloorHeight;
            float ceilingB = sectorB.CeilingHeight;

            // Draw upper wall section if there's a ceiling height difference
            if (Math.Abs(ceilingA - ceilingB) > 0.01f)
            {
                float upperBottom = Math.Min(ceilingA, ceilingB);
                float upperTop = Math.Max(ceilingA, ceilingB);
                Draw3DWall(wall.Start, wall.End, upperBottom, upperTop, wallTexture);
            }

            // Draw lower wall section if there's a floor height difference
            if (Math.Abs(floorA - floorB) > 0.01f)
            {
                float lowerBottom = Math.Min(floorA, floorB);
                float lowerTop = Math.Max(floorA, floorB);
                Draw3DWall(wall.Start, wall.End, lowerBottom, lowerTop, wallTexture);
            }

            // No middle section is drawn - this creates the opening effect
            // The adjacent sector will be drawn separately, making it visible through the opening
        }

        private void DrawNestedSectorHeightTransitionWall(Wall wall, Sector nestedSector, string wallTexture)
        {
            // First check if this wall borders another nested sector
            Sector adjacentSector = null;

            // Check if any point on this wall is inside another nested sector
            Vector2 wallMidpoint = (wall.Start + wall.End) * 0.5f;
            Vector2 wallDirection = Vector2.Normalize(wall.End - wall.Start);
            Vector2 perpendicular = new Vector2(-wallDirection.Y, wallDirection.X); // Perpendicular to wall

            // Check slightly on the other side of the wall
            Vector2 otherSidePoint = wallMidpoint + perpendicular * 0.1f;
            adjacentSector = _sectors.FirstOrDefault(s => s.IsNested && s != nestedSector && IsPointInSector(otherSidePoint, s));

            // If no nested sector found on the other side, try the opposite direction
            if (adjacentSector == null)
            {
                otherSidePoint = wallMidpoint - perpendicular * 0.1f;
                adjacentSector = _sectors.FirstOrDefault(s => s.IsNested && s != nestedSector && IsPointInSector(otherSidePoint, s));
            }

            // Determine which sector to compare heights with
            Sector compareSector;
            if (adjacentSector != null)
            {
                // Wall is between two nested sectors - compare with the adjacent nested sector
                compareSector = adjacentSector;
                Console.WriteLine($"Wall between nested sectors {nestedSector.Id} and {adjacentSector.Id}");
            }
            else
            {
                // Wall borders parent sector - use parent sector for comparison
                if (!nestedSector.ParentSectorId.HasValue)
                    return;

                compareSector = _sectors.FirstOrDefault(s => s.Id == nestedSector.ParentSectorId.Value);
                if (compareSector == null)
                    return;
            }

            // Check if nested sector has slopes - if so, use sloped wall rendering
            if (nestedSector.HasSlopes)
            {
                // TODO: Update sloped wall rendering for textures
                // DrawNestedSectorSlopedHeightTransitionWall(wall, nestedSector, parentSector, wallTexture);
                return;
            }

            // Only draw transition walls when there are actual height differences
            // Include AnimationHeightOffset to handle moving lifts properly
            float nestedFloorHeight = nestedSector.FloorHeight + nestedSector.AnimationHeightOffset;
            float nestedCeilingHeight = nestedSector.CeilingHeight + (nestedSector.IsLift ? 0 : nestedSector.AnimationHeightOffset);
            float compareFloorHeight = compareSector.FloorHeight + compareSector.AnimationHeightOffset;
            float compareCeilingHeight = compareSector.CeilingHeight + (compareSector.IsLift ? 0 : compareSector.AnimationHeightOffset);

            bool hasFloorDifference = Math.Abs(nestedFloorHeight - compareFloorHeight) > 0.1f;
            bool hasCeilingDifference = Math.Abs(nestedCeilingHeight - compareCeilingHeight) > 0.1f;

            // If no height differences, don't draw any walls (this creates the "pit" effect)
            if (!hasFloorDifference && !hasCeilingDifference)
                return;

            // Draw floor transition wall (from compare sector floor to nested floor)
            if (hasFloorDifference)
            {
                Draw3DWall(wall.Start, wall.End, compareFloorHeight, nestedFloorHeight, wallTexture);
            }

            // Draw ceiling transition wall (from nested ceiling to compare sector ceiling)
            if (hasCeilingDifference)
            {
                Draw3DWall(wall.Start, wall.End, nestedCeilingHeight, compareCeilingHeight, wallTexture);
            }
        }

        private void DrawNestedSectorSlopedHeightTransitionWall(Wall wall, Sector nestedSector, Sector parentSector,
            string wallTexture)
        {
            // TODO: Implement textured sloped wall rendering
            // This method needs to be updated to use VertexPositionTexture instead of VertexPositionColor
            // For now, skip sloped walls to get basic texture rendering working
        }

        private void Draw3DSlopedWall(Vector2 start, Vector2 end, Sector sector, string textureName)
        {
            var texture = GetTexture(textureName);
            if (texture == null) return;

            _basicEffect.Texture = texture;

            // Set sampler state to enable texture wrapping/tiling
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            // Find vertex indices for the wall endpoints
            int startIndex = sector.Vertices.IndexOf(start);
            int endIndex = sector.Vertices.IndexOf(end);

            // Get heights at each end of the wall
            float startFloorHeight = GetVertexHeight(sector, startIndex, true);
            float startCeilingHeight = GetVertexHeight(sector, startIndex, false);
            float endFloorHeight = GetVertexHeight(sector, endIndex, true);
            float endCeilingHeight = GetVertexHeight(sector, endIndex, false);

            // Calculate wall length for proper UV tiling
            float wallLength = Vector2.Distance(start, end);
            
            // Texture tiling: repeat texture based on configurable world units (like Build engine)
            var textureScale = TextureScaleSettings.GetWallTextureScaleVector(textureName);
            float textureWorldWidth = textureScale.X;   // World units per texture repeat horizontally
            float textureWorldHeight = textureScale.Y;  // World units per texture repeat vertically
            
            float uRepeat = wallLength / textureWorldWidth;
            
            // For sloped walls, calculate the height at each end separately
            float startWallHeight = startCeilingHeight - startFloorHeight;
            float endWallHeight = endCeilingHeight - endFloorHeight;
            
            float startVRepeat = startWallHeight / textureWorldHeight;
            float endVRepeat = endWallHeight / textureWorldHeight;

            var vertices = new VertexPositionTexture[4];

            // Create wall quad with sloped heights and proper UV mapping
            vertices[0] = new VertexPositionTexture(new Vector3(start.X, startFloorHeight, -start.Y), new Vector2(0, startVRepeat)); // Bottom left
            vertices[1] = new VertexPositionTexture(new Vector3(start.X, startCeilingHeight, -start.Y), new Vector2(0, 0)); // Top left
            vertices[2] = new VertexPositionTexture(new Vector3(end.X, endCeilingHeight, -end.Y), new Vector2(uRepeat, 0)); // Top right
            vertices[3] = new VertexPositionTexture(new Vector3(end.X, endFloorHeight, -end.Y), new Vector2(uRepeat, endVRepeat)); // Bottom right

            var indices = new short[] { 0, 1, 2, 0, 2, 3 };

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
            }
        }

        private void Draw3DFloorCeiling(Sector sector)
        {
            if (sector.Vertices.Count < 3) return;

            var floorColor = GetTextureColor(sector.FloorTexture);
            var ceilingColor = GetTextureColor(sector.CeilingTexture);

            // Get triangles with holes cut out for nested sectors
            var triangles = TriangulateSectorWithHoles(sector);

            foreach (var triangle in triangles)
            {
                // Check if this sector has slopes
                if (sector.HasSlopes)
                {
                    // Draw sloped floor and ceiling using per-vertex heights
                    Draw3DSlopedTriangle(triangle[0], triangle[1], triangle[2], sector, true, sector.FloorTexture); // Floor
                    Draw3DSlopedTriangle(triangle[2], triangle[1], triangle[0], sector, false,
                        sector.CeilingTexture); // Ceiling (reverse winding)
                }
                else
                {
                    // Draw flat floor and ceiling
                    float animatedFloorHeight = sector.FloorHeight + sector.AnimationHeightOffset;
                    Draw3DTriangle(triangle[0], triangle[1], triangle[2], animatedFloorHeight, sector.FloorTexture);
                    Draw3DTriangle(triangle[2], triangle[1], triangle[0], sector.CeilingHeight, sector.CeilingTexture);
                }
            }
        }

        private void Draw3DNestedFloorCeiling(Sector sector)
        {
            if (sector.Vertices.Count < 3) return;

            var floorColor = GetTextureColor(sector.FloorTexture);
            var ceilingColor = GetTextureColor(sector.CeilingTexture);

            // Triangulate the sector polygon for floor and ceiling
            var triangles = TriangulateSector(sector.Vertices);

            // Small offset to ensure nested floors render on top of parent floors
            const float heightOffset = 0.01f;

            foreach (var triangle in triangles)
            {
                // Check if this sector has slopes
                if (sector.HasSlopes)
                {
                    // Draw sloped floor and ceiling using per-vertex heights with offset
                    Draw3DNestedSlopedTriangle(triangle[0], triangle[1], triangle[2], sector, true, sector.FloorTexture,
                        heightOffset); // Floor
                    Draw3DNestedSlopedTriangle(triangle[2], triangle[1], triangle[0], sector, false, sector.CeilingTexture,
                        -heightOffset); // Ceiling (reverse winding, negative offset)
                }
                else
                {
                    // Draw flat floor and ceiling with height offset
                    float animatedFloorHeight = sector.FloorHeight + sector.AnimationHeightOffset + heightOffset;
                    float ceilingHeight = sector.CeilingHeight - heightOffset;
                    Draw3DTriangle(triangle[0], triangle[1], triangle[2], animatedFloorHeight, sector.FloorTexture);
                    Draw3DTriangle(triangle[2], triangle[1], triangle[0], ceilingHeight, sector.CeilingTexture);
                }
            }
        }

        private void Draw3DNestedSlopedTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Sector sector, bool isFloor,
            string textureName, float heightOffset)
        {
            var texture = GetTexture(textureName);
            if (texture == null) return;

            _basicEffect.Texture = texture;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            // Find vertex indices
            int v1Index = sector.Vertices.IndexOf(v1);
            int v2Index = sector.Vertices.IndexOf(v2);
            int v3Index = sector.Vertices.IndexOf(v3);

            // Get heights at each vertex with offset
            float h1 = GetVertexHeight(sector, v1Index, isFloor) + heightOffset;
            float h2 = GetVertexHeight(sector, v2Index, isFloor) + heightOffset;
            float h3 = GetVertexHeight(sector, v3Index, isFloor) + heightOffset;

            // Calculate UV coordinates based on world position for tiling with configurable scale
            float textureWorldSize = TextureScaleSettings.GetFloorCeilingTextureScale(textureName, isFloor);
            Vector2 uv1 = new Vector2(v1.X / textureWorldSize, v1.Y / textureWorldSize);
            Vector2 uv2 = new Vector2(v2.X / textureWorldSize, v2.Y / textureWorldSize);
            Vector2 uv3 = new Vector2(v3.X / textureWorldSize, v3.Y / textureWorldSize);

            var vertices = new VertexPositionTexture[3];
            vertices[0] = new VertexPositionTexture(new Vector3(v1.X, h1, -v1.Y), uv1);
            vertices[1] = new VertexPositionTexture(new Vector3(v2.X, h2, -v2.Y), uv2);
            vertices[2] = new VertexPositionTexture(new Vector3(v3.X, h3, -v3.Y), uv3);

            var indices = new short[] { 0, 1, 2 };

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 3, indices, 0, 1);
            }
        }

        private bool IsTriangleInsideNestedSector(Vector2[] triangle, Sector parentSector)
        {
            // Find all nested sectors within this parent sector
            var nestedSectors = _sectors.Where(s => s.IsNested && s.ParentSectorId == parentSector.Id).ToList();

            foreach (var nestedSector in nestedSectors)
            {
                // Check if the triangle's center point is inside any nested sector
                var triangleCenter = (triangle[0] + triangle[1] + triangle[2]) / 3f;
                if (IsPointInSector(triangleCenter, nestedSector))
                {
                    return true;
                }
            }

            return false;
        }

        private List<Vector2[]> TriangulateSector(List<Vector2> vertices)
        {
            var triangles = new List<Vector2[]>();
            if (vertices.Count < 3) return triangles;

            // Simple fan triangulation from first vertex
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                triangles.Add(new Vector2[] { vertices[0], vertices[i], vertices[i + 1] });
            }

            return triangles;
        }

        private List<Vector2[]> TriangulateSectorWithHoles(Sector sector)
        {
            if (sector.Vertices.Count < 3) return new List<Vector2[]>();

            // Find all nested sectors that are inside this parent sector
            var nestedSectors = _sectors.Where(s => s.IsNested && IsNestedSectorInsideParent(s, sector)).ToList();

            if (nestedSectors.Count == 0)
            {
                // No holes, use simple triangulation
                return TriangulateSector(sector.Vertices);
            }

            // Create a constrained triangulation with holes
            return TriangulatePolygonWithHoles(sector.Vertices, nestedSectors.Select(s => s.Vertices).ToList());
        }

        private bool IsNestedSectorInsideParent(Sector nestedSector, Sector parentSector)
        {
            if (nestedSector.Vertices.Count == 0) return false;

            // Check if any vertex of the nested sector is inside the parent sector
            foreach (var vertex in nestedSector.Vertices)
            {
                if (IsPointInPolygon(vertex, parentSector.Vertices))
                {
                    return true;
                }
            }

            return false;
        }


        private List<Vector2[]> TriangulatePolygonWithHoles(List<Vector2> outerVertices, List<List<Vector2>> holes)
        {
            var triangles = new List<Vector2[]>();

            // Create a combined vertex list: outer boundary + all hole boundaries
            var allVertices = new List<Vector2>(outerVertices);
            var constraintEdges = new List<(int, int)>();

            // Add outer boundary constraint edges
            for (int i = 0; i < outerVertices.Count; i++)
            {
                constraintEdges.Add((i, (i + 1) % outerVertices.Count));
            }

            // Add hole vertices and constraint edges
            foreach (var hole in holes)
            {
                int holeStartIndex = allVertices.Count;
                allVertices.AddRange(hole);

                // Add hole boundary constraint edges
                for (int i = 0; i < hole.Count; i++)
                {
                    int current = holeStartIndex + i;
                    int next = holeStartIndex + ((i + 1) % hole.Count);
                    constraintEdges.Add((current, next));
                }
            }

            // Create constrained triangulation
            var constrainedTriangles = CreateConstrainedTriangulation(allVertices, constraintEdges, holes);

            return constrainedTriangles;
        }

        private List<Vector2[]> CreateConstrainedTriangulation(List<Vector2> vertices, List<(int, int)> constraintEdges,
            List<List<Vector2>> holes)
        {
            var triangles = new List<Vector2[]>();

            // Start with Delaunay-like triangulation but respect constraint edges
            // This is a simplified implementation - for production, use a proper constrained Delaunay library

            // Create initial triangulation of all vertices
            var initialTriangles = TriangulatePointSet(vertices);

            // Filter triangles and ensure constraint edges are respected
            foreach (var triangle in initialTriangles)
            {
                Vector2 centroid = new Vector2(
                    (triangle[0].X + triangle[1].X + triangle[2].X) / 3f,
                    (triangle[0].Y + triangle[1].Y + triangle[2].Y) / 3f
                );

                // Check if triangle centroid is inside any hole
                bool isInHole = false;
                foreach (var hole in holes)
                {
                    if (IsPointInPolygon(centroid, hole))
                    {
                        isInHole = true;
                        break;
                    }
                }

                // Only include triangles that are not inside holes
                if (!isInHole)
                {
                    // Check if triangle respects constraint edges
                    if (TriangleRespectsConstraints(triangle, vertices, constraintEdges))
                    {
                        triangles.Add(triangle);
                    }
                }
            }

            return triangles;
        }

        private List<Vector2[]> TriangulatePointSet(List<Vector2> vertices)
        {
            var triangles = new List<Vector2[]>();

            if (vertices.Count < 3) return triangles;

            // Use a more sophisticated triangulation than simple fan
            // This creates a better distribution of triangles

            // Find the bounding box to create initial super-triangle
            float minX = vertices.Min(v => v.X) - 100;
            float maxX = vertices.Max(v => v.X) + 100;
            float minY = vertices.Min(v => v.Y) - 100;
            float maxY = vertices.Max(v => v.Y) + 100;

            // Create super triangle that contains all points
            var superTriangle = new Vector2[]
            {
                new Vector2(minX - (maxX - minX), minY - (maxY - minY)),
                new Vector2(maxX + (maxX - minX), minY - (maxY - minY)),
                new Vector2((minX + maxX) / 2, maxY + (maxY - minY) * 2)
            };

            var workingTriangles = new List<Vector2[]> { superTriangle };

            // Insert each vertex
            foreach (var vertex in vertices)
            {
                var newTriangles = new List<Vector2[]>();
                var edges = new List<(Vector2, Vector2)>();

                // Find triangles whose circumcircle contains the vertex
                foreach (var triangle in workingTriangles)
                {
                    if (IsPointInCircumcircle(vertex, triangle))
                    {
                        // Add triangle edges to edge list
                        edges.Add((triangle[0], triangle[1]));
                        edges.Add((triangle[1], triangle[2]));
                        edges.Add((triangle[2], triangle[0]));
                    }
                    else
                    {
                        newTriangles.Add(triangle);
                    }
                }

                // Remove duplicate edges
                var uniqueEdges = new List<(Vector2, Vector2)>();
                foreach (var edge in edges)
                {
                    var reverseEdge = (edge.Item2, edge.Item1);
                    if (edges.Count(e => (e.Item1 == edge.Item1 && e.Item2 == edge.Item2) ||
                                         (e.Item1 == edge.Item2 && e.Item2 == edge.Item1)) == 1)
                    {
                        uniqueEdges.Add(edge);
                    }
                }

                // Create new triangles with the vertex
                foreach (var edge in uniqueEdges)
                {
                    newTriangles.Add(new Vector2[] { vertex, edge.Item1, edge.Item2 });
                }

                workingTriangles = newTriangles;
            }

            // Remove triangles that contain super-triangle vertices
            var finalTriangles = new List<Vector2[]>();
            foreach (var triangle in workingTriangles)
            {
                bool containsSuperVertex = false;
                foreach (var vertex in triangle)
                {
                    if (superTriangle.Contains(vertex))
                    {
                        containsSuperVertex = true;
                        break;
                    }
                }

                if (!containsSuperVertex)
                {
                    finalTriangles.Add(triangle);
                }
            }

            return finalTriangles;
        }

        private bool IsPointInCircumcircle(Vector2 point, Vector2[] triangle)
        {
            // Calculate circumcircle and test if point is inside
            Vector2 a = triangle[0];
            Vector2 b = triangle[1];
            Vector2 c = triangle[2];

            float d = 2 * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));

            if (Math.Abs(d) < 0.0001f) return false; // Degenerate triangle

            float ux = ((a.X * a.X + a.Y * a.Y) * (b.Y - c.Y) +
                        (b.X * b.X + b.Y * b.Y) * (c.Y - a.Y) +
                        (c.X * c.X + c.Y * c.Y) * (a.Y - b.Y)) / d;

            float uy = ((a.X * a.X + a.Y * a.Y) * (c.X - b.X) +
                        (b.X * b.X + b.Y * b.Y) * (a.X - c.X) +
                        (c.X * c.X + c.Y * c.Y) * (b.X - a.X)) / d;

            Vector2 circumcenter = new Vector2(ux, uy);
            float radiusSquared = (circumcenter - a).LengthSquared();
            float distanceSquared = (point - circumcenter).LengthSquared();

            return distanceSquared < radiusSquared;
        }

        private bool TriangleRespectsConstraints(Vector2[] triangle, List<Vector2> allVertices,
            List<(int, int)> constraintEdges)
        {
            // Check if triangle edges intersect with any constraint edges inappropriately
            for (int i = 0; i < 3; i++)
            {
                Vector2 edgeStart = triangle[i];
                Vector2 edgeEnd = triangle[(i + 1) % 3];

                foreach (var (startIdx, endIdx) in constraintEdges)
                {
                    Vector2 constraintStart = allVertices[startIdx];
                    Vector2 constraintEnd = allVertices[endIdx];

                    // Skip if triangle edge shares vertices with constraint edge
                    if ((edgeStart == constraintStart || edgeStart == constraintEnd) &&
                        (edgeEnd == constraintStart || edgeEnd == constraintEnd))
                    {
                        continue;
                    }

                    // Check for intersection
                    if (LineSegmentsIntersect(edgeStart, edgeEnd, constraintStart, constraintEnd))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool LineSegmentsIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            float d1 = CrossProduct(q2 - p2, p1 - p2);
            float d2 = CrossProduct(q2 - p2, q1 - p2);
            float d3 = CrossProduct(q1 - p1, p2 - p1);
            float d4 = CrossProduct(q1 - p1, q2 - p1);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            return false;
        }

        private float CrossProduct(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        private void Draw3DTriangle(Vector2 v1, Vector2 v2, Vector2 v3, float height, string textureName)
        {
            var texture = GetTexture(textureName);
            if (texture == null) return;

            _basicEffect.Texture = texture;

            // Set sampler state to enable texture wrapping/tiling
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            var vertices = new VertexPositionTexture[3];

            // For floor/ceiling textures, map world coordinates directly to UV coordinates
            // This creates a flat, tiled surface like Build engine
            float textureWorldSize = TextureScaleSettings.GetFloorCeilingTextureScale(textureName, true); // Generic triangle, assume floor
            
            // Calculate UV coordinates based on world position
            Vector2 uv1 = new Vector2(v1.X / textureWorldSize, v1.Y / textureWorldSize);
            Vector2 uv2 = new Vector2(v2.X / textureWorldSize, v2.Y / textureWorldSize);  
            Vector2 uv3 = new Vector2(v3.X / textureWorldSize, v3.Y / textureWorldSize);

            vertices[0] = new VertexPositionTexture(new Vector3(v1.X, height, -v1.Y), uv1);
            vertices[1] = new VertexPositionTexture(new Vector3(v2.X, height, -v2.Y), uv2);
            vertices[2] = new VertexPositionTexture(new Vector3(v3.X, height, -v3.Y), uv3);

            var indices = new short[] { 0, 1, 2 };

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 3, indices, 0, 1);
            }
        }

        private void Draw3DSlopedTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Sector sector, bool isFloor, string textureName)
        {
            var texture = GetTexture(textureName);
            if (texture == null) return;

            _basicEffect.Texture = texture;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            // Get vertex indices in the sector
            int v1Index = sector.Vertices.IndexOf(v1);
            int v2Index = sector.Vertices.IndexOf(v2);
            int v3Index = sector.Vertices.IndexOf(v3);

            // Get heights for each vertex
            float h1 = GetVertexHeight(sector, v1Index, isFloor);
            float h2 = GetVertexHeight(sector, v2Index, isFloor);
            float h3 = GetVertexHeight(sector, v3Index, isFloor);

            // Apply animation offset to floor
            if (isFloor)
            {
                h1 += sector.AnimationHeightOffset;
                h2 += sector.AnimationHeightOffset;
                h3 += sector.AnimationHeightOffset;
            }

            // Calculate UV coordinates based on world position for tiling with configurable scale
            float textureWorldSize = TextureScaleSettings.GetFloorCeilingTextureScale(textureName, isFloor);
            Vector2 uv1 = new Vector2(v1.X / textureWorldSize, v1.Y / textureWorldSize);
            Vector2 uv2 = new Vector2(v2.X / textureWorldSize, v2.Y / textureWorldSize);
            Vector2 uv3 = new Vector2(v3.X / textureWorldSize, v3.Y / textureWorldSize);

            var vertices = new VertexPositionTexture[3];
            vertices[0] = new VertexPositionTexture(new Vector3(v1.X, h1, -v1.Y), uv1);
            vertices[1] = new VertexPositionTexture(new Vector3(v2.X, h2, -v2.Y), uv2);
            vertices[2] = new VertexPositionTexture(new Vector3(v3.X, h3, -v3.Y), uv3);

            var indices = new short[] { 0, 1, 2 };

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 3, indices, 0, 1);
            }
        }






        private void DrawSprite3D(Sprite sprite, Sector sector)
        {
            if (!sprite.Visible) return;

            // Get sprite color based on tag
            Color spriteColor = sprite.Tag switch
            {
                SpriteTag.Switch => Color.Magenta,
                SpriteTag.Decoration => Color.Cyan,
                _ => Color.White
            };

            float spriteSize = 32f * sprite.Scale.X; // Base size scaled by sprite's scale
            Vector3 spritePosition;

            // Position sprite based on alignment - now using Height property for proper Y positioning
            switch (sprite.Alignment)
            {
                case SpriteAlignment.Floor:
                    // Sprite sits on the floor - use Height as offset from floor
                    spritePosition = new Vector3(sprite.Position.X, sector.FloorHeight + sprite.Height,
                        -sprite.Position.Y);
                    break;

                case SpriteAlignment.Wall:
                    // Wall sprite - use Height as offset from floor
                    spritePosition = new Vector3(sprite.Position.X, sector.FloorHeight + sprite.Height,
                        -sprite.Position.Y);
                    break;

                case SpriteAlignment.Face:
                default:
                    // Billboard sprite - use Height as offset from floor
                    spritePosition = new Vector3(sprite.Position.X, sector.FloorHeight + sprite.Height,
                        -sprite.Position.Y);
                    break;
            }

            // Draw sprite as a simple billboard quad with yaw and pitch rotation
            var pitch = sprite.Properties.ContainsKey("Pitch") ? (float)sprite.Properties["Pitch"] : 0f;
            // Debug: show sprite info before rendering
            
            DrawSpriteBillboard(spritePosition, spriteSize, spriteColor, sprite.Alignment, sprite.Angle, pitch);

            // Draw selection highlight in 3D - DISABLED
            // if (_selectedSprite == sprite)
            // {
            //     DrawSpriteBillboard(spritePosition, spriteSize + 8, Color.White * 0.5f, SpriteAlignment.Face);
            // }
        }

        private void DrawSpriteBillboard(Vector3 position, float size, Color color, SpriteAlignment alignment,
            float yaw = 0f, float pitch = 0f)
        {
            Vector3 cameraToSprite = position - _camera3DPosition;
            Vector3 right, up;

            if (alignment == SpriteAlignment.Face)
            {
                // Billboard - always faces camera
                right = Vector3.Cross(Vector3.Up, cameraToSprite);
                right.Normalize();
                up = Vector3.Up;
            }
            else if (alignment == SpriteAlignment.Wall)
            {
                // Wall-aligned sprite - compute tangent (along wall) and normal (perpendicular to wall)
                // yaw represents the wall's direction angle from Atan2(dy, dx)
                float wallAngleRad = MathHelper.ToRadians(yaw);
                
                // Map from 2D map coordinates to 3D rendering space
                // Build engine: 2D map (x,y) → 3D space (x, Y=up, -z for proper orientation)
                // The yaw angle comes from Atan2(dy, dx) in 2D map space
                
                // Wall tangent vector (along the wall direction) - map 2D to 3D with Z negated
                Vector3 wallTangent = Vector3.Normalize(new Vector3((float)Math.Cos(wallAngleRad), 0, -(float)Math.Sin(wallAngleRad)));
                
                // Wall normal vector (perpendicular to wall, pointing outward) using correct cross product order
                Vector3 wallNormal = Vector3.Normalize(Vector3.Cross(Vector3.Up, wallTangent));
                
                
                // Use tangent for sprite's right axis and normal for positioning offset
                right = wallTangent;
                up = Vector3.Up;
                
                // Offset sprite position slightly outward along wall normal to avoid z-fighting
                position = position + wallNormal * 0.05f;
            }
            else // Floor alignment
            {
                // Floor-aligned sprite - lies flat on ground
                right = new Vector3(1, 0, 0);
                up = new Vector3(0, 0, 1);
            }

            // Apply rotation if specified (skip for wall sprites which already have correct orientation)
            if ((yaw != 0f || pitch != 0f) && alignment != SpriteAlignment.Wall)
            {
                var yawRad = MathHelper.ToRadians(yaw);
                var pitchRad = MathHelper.ToRadians(pitch);

                // Apply yaw rotation (around Y-axis)
                if (yaw != 0f)
                {
                    var cosYaw = (float)Math.Cos(yawRad);
                    var sinYaw = (float)Math.Sin(yawRad);

                    var originalRight = right;
                    right = new Vector3(
                        originalRight.X * cosYaw - originalRight.Z * sinYaw,
                        originalRight.Y,
                        originalRight.X * sinYaw + originalRight.Z * cosYaw
                    );
                }

                // Apply pitch rotation (around X-axis)
                if (pitch != 0f)
                {
                    var cosPitch = (float)Math.Cos(pitchRad);
                    var sinPitch = (float)Math.Sin(pitchRad);

                    var originalUp = up;
                    up = new Vector3(
                        originalUp.X,
                        originalUp.Y * cosPitch - originalUp.Z * sinPitch,
                        originalUp.Y * sinPitch + originalUp.Z * cosPitch
                    );
                }
            }

            float halfSize = size / 2;

            var vertices = new VertexPositionColor[4];
            vertices[0] = new VertexPositionColor(position - right * halfSize - up * halfSize, color);
            vertices[1] = new VertexPositionColor(position + right * halfSize - up * halfSize, color);
            vertices[2] = new VertexPositionColor(position - right * halfSize + up * halfSize, color);
            vertices[3] = new VertexPositionColor(position + right * halfSize + up * halfSize, color);

            var indices = new short[] { 0, 1, 2, 1, 3, 2 };

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
            }
        }

        private bool IsPointInSector(Vector2 point, Sector sector)
        {
            if (sector.Vertices.Count < 3) return false;

            // Use ray casting algorithm to determine if point is inside polygon
            int intersections = 0;
            var vertices = sector.Vertices;

            for (int i = 0; i < vertices.Count; i++)
            {
                var v1 = vertices[i];
                var v2 = vertices[(i + 1) % vertices.Count];

                if (((v1.Y > point.Y) != (v2.Y > point.Y)) &&
                    (point.X < (v2.X - v1.X) * (point.Y - v1.Y) / (v2.Y - v1.Y) + v1.X))
                {
                    intersections++;
                }
            }

            return (intersections % 2) == 1;
        }

        private void Draw3DCursor()
        {
            // Always draw a mouse ray indicator in 3D mode
            var mouseState = Mouse.GetState();
            var viewportBounds = GetViewportBounds();

            if (viewportBounds.Contains(mouseState.Position))
            {
                // Draw a ray from mouse position into the scene
                var ray = ScreenPointToRay(mouseState.Position);
                var rayEnd = ray.Position + ray.Direction * 1000f; // Long ray

                var rayVertices = new VertexPositionColor[2];
                rayVertices[0] = new VertexPositionColor(ray.Position, Color.Cyan * 0.5f);
                rayVertices[1] = new VertexPositionColor(rayEnd, Color.Cyan * 0.1f);

                foreach (var pass in _basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, rayVertices, 0, 1);
                }
            }

         
            // Draw gizmos for selected sprite
            if (_selectedSprite3D != null)
            {
                var spritePos = Get3DSpriteWorldPosition(_selectedSprite3D);
                DrawGizmos(spritePos);
                return; // Don't draw regular cursor when sprite is selected
            }

            // Draw snapped cursor position if we have a valid snap
            if (_cursor3DSnapType == "none") return;

            // Standard cursor colors based on snap type
            Color cursorColor = _cursor3DSnapType switch
            {
                "floor" => Color.Green,
                "ceiling" => Color.Blue,
                "wall" => Color.Red,
                _ => Color.White
            };
            float size = 8f;

            // Draw cursor as a small cross at the intersection point
            var vertices = new VertexPositionColor[8]; // 4 lines, 2 vertices each

            // X-axis line
            vertices[0] = new VertexPositionColor(_cursor3DPosition - Vector3.Right * size, cursorColor);
            vertices[1] = new VertexPositionColor(_cursor3DPosition + Vector3.Right * size, cursorColor);

            // Y-axis line
            vertices[2] = new VertexPositionColor(_cursor3DPosition - Vector3.Up * size, cursorColor);
            vertices[3] = new VertexPositionColor(_cursor3DPosition + Vector3.Up * size, cursorColor);

            // Z-axis line
            vertices[4] = new VertexPositionColor(_cursor3DPosition - Vector3.Forward * size, cursorColor);
            vertices[5] = new VertexPositionColor(_cursor3DPosition + Vector3.Forward * size, cursorColor);

            // Normal indicator (shows surface direction)
            vertices[6] = new VertexPositionColor(_cursor3DPosition, cursorColor);
            vertices[7] = new VertexPositionColor(_cursor3DPosition + _cursor3DNormal * size * 2, Color.Yellow);

            // Draw the cursor lines
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 4);
            }

        
        }

        private void DrawCircle(Vector2 center, float radius, Color color)
        {
            var rect = new Rectangle((int)(center.X - radius), (int)(center.Y - radius),
                (int)(radius * 2), (int)(radius * 2));
            _spriteBatch.Draw(_pixelTexture, rect, color);
        }

        private void DrawSpriteSelectionBox(Vector3 position, Color color)
        {
            var size = 6f; // Much smaller box
            var vertices = new VertexPositionColor[24]; // Full wireframe box

            // Always center the box on the sprite position for consistency
            var boxCenter = position;

            // Bottom face
            vertices[0] = new VertexPositionColor(boxCenter + new Vector3(-size, -size, -size), color);
            vertices[1] = new VertexPositionColor(boxCenter + new Vector3(size, -size, -size), color);
            vertices[2] = new VertexPositionColor(boxCenter + new Vector3(size, -size, -size), color);
            vertices[3] = new VertexPositionColor(boxCenter + new Vector3(size, -size, size), color);
            vertices[4] = new VertexPositionColor(boxCenter + new Vector3(size, -size, size), color);
            vertices[5] = new VertexPositionColor(boxCenter + new Vector3(-size, -size, size), color);
            vertices[6] = new VertexPositionColor(boxCenter + new Vector3(-size, -size, size), color);
            vertices[7] = new VertexPositionColor(boxCenter + new Vector3(-size, -size, -size), color);

            // Top face
            vertices[8] = new VertexPositionColor(boxCenter + new Vector3(-size, size, -size), color);
            vertices[9] = new VertexPositionColor(boxCenter + new Vector3(size, size, -size), color);
            vertices[10] = new VertexPositionColor(boxCenter + new Vector3(size, size, -size), color);
            vertices[11] = new VertexPositionColor(boxCenter + new Vector3(size, size, size), color);
            vertices[12] = new VertexPositionColor(boxCenter + new Vector3(size, size, size), color);
            vertices[13] = new VertexPositionColor(boxCenter + new Vector3(-size, size, size), color);
            vertices[14] = new VertexPositionColor(boxCenter + new Vector3(-size, size, size), color);
            vertices[15] = new VertexPositionColor(boxCenter + new Vector3(-size, size, -size), color);

            // Vertical edges
            vertices[16] = new VertexPositionColor(boxCenter + new Vector3(-size, -size, -size), color);
            vertices[17] = new VertexPositionColor(boxCenter + new Vector3(-size, size, -size), color);
            vertices[18] = new VertexPositionColor(boxCenter + new Vector3(size, -size, -size), color);
            vertices[19] = new VertexPositionColor(boxCenter + new Vector3(size, size, -size), color);
            vertices[20] = new VertexPositionColor(boxCenter + new Vector3(size, -size, size), color);
            vertices[21] = new VertexPositionColor(boxCenter + new Vector3(size, size, size), color);
            vertices[22] = new VertexPositionColor(boxCenter + new Vector3(-size, -size, size), color);
            vertices[23] = new VertexPositionColor(boxCenter + new Vector3(-size, size, size), color);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 12);
            }
        }

        private string GetGizmoAxisUnderMouse(Vector3 gizmoPosition, Ray ray)
        {
            var gizmoSize = 20f;
            var hitThreshold = 4f;

            // Check X-axis (Red)
            var xEnd = gizmoPosition + Vector3.Right * gizmoSize;
            if (DistanceFromRayToLine(ray, gizmoPosition, xEnd) < hitThreshold)
                return "X";

            // Check Y-axis (Green)
            var yEnd = gizmoPosition + Vector3.Up * gizmoSize;
            if (DistanceFromRayToLine(ray, gizmoPosition, yEnd) < hitThreshold)
                return "Y";

            // Check Z-axis (Blue)
            var zEnd = gizmoPosition + Vector3.Forward * gizmoSize;
            if (DistanceFromRayToLine(ray, gizmoPosition, zEnd) < hitThreshold)
                return "Z";

            return "none";
        }

        private float DistanceFromRayToLine(Ray ray, Vector3 lineStart, Vector3 lineEnd)
        {
            var lineDirection = Vector3.Normalize(lineEnd - lineStart);
            var rayToLineStart = lineStart - ray.Position;

            var cross = Vector3.Cross(ray.Direction, lineDirection);
            if (cross.Length() < 0.001f) return float.MaxValue; // Parallel lines

            var distance = Math.Abs(Vector3.Dot(rayToLineStart, cross)) / cross.Length();
            return distance;
        }

        private void DrawGizmos(Vector3 position)
        {
            var gizmoSize = 20f;
            var thickness = 2f;

            // X-axis (Red)
            var xVertices = new VertexPositionColor[2];
            xVertices[0] = new VertexPositionColor(position, Color.Red);
            xVertices[1] = new VertexPositionColor(position + Vector3.Right * gizmoSize, Color.Red);

            // Y-axis (Green) 
            var yVertices = new VertexPositionColor[2];
            yVertices[0] = new VertexPositionColor(position, Color.Green);
            yVertices[1] = new VertexPositionColor(position + Vector3.Up * gizmoSize, Color.Green);

            // Z-axis (Blue)
            var zVertices = new VertexPositionColor[2];
            zVertices[0] = new VertexPositionColor(position, Color.Blue);
            zVertices[1] = new VertexPositionColor(position + Vector3.Forward * gizmoSize, Color.Blue);

            // Draw all axes
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, xVertices, 0, 1);
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, yVertices, 0, 1);
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, zVertices, 0, 1);
            }
        }

        // Helper methods for nested sector system
        private Vector2 GetCenterOfVertices(List<Vector2> vertices)
        {
            if (vertices.Count == 0) return Vector2.Zero;

            var sum = Vector2.Zero;
            foreach (var vertex in vertices)
                sum += vertex;
            return sum / vertices.Count;
        }

        private Sector FindParentSectorForPoint(Vector2 point)
        {
            // Find the sector that contains this point (excluding the current sector being created)
            foreach (var sector in _sectors.Take(_sectors.Count - 1)) // Exclude the sector currently being created
            {
                if (!sector.IsNested && IsPointInSector(point, sector))
                    return sector;
            }

            return null;
        }



        private void SetNestedSectorHeights(Sector nestedSector, Sector parentSector)
        {
            const float defaultOffset = 64f; // Build Engine units for height difference

            switch (nestedSector.SectorType)
            {
                case SectorType.FloorPit:
                    nestedSector.FloorHeight = parentSector.FloorHeight - defaultOffset;
                    nestedSector.CeilingHeight = parentSector.CeilingHeight;
                    break;

                case SectorType.FloorRaise:
                    nestedSector.FloorHeight = parentSector.FloorHeight + defaultOffset;
                    nestedSector.CeilingHeight = parentSector.CeilingHeight;
                    break;

                case SectorType.CeilingLower:
                    nestedSector.FloorHeight = parentSector.FloorHeight;
                    nestedSector.CeilingHeight = parentSector.CeilingHeight - defaultOffset;
                    break;

                case SectorType.CeilingRaise:
                    nestedSector.FloorHeight = parentSector.FloorHeight;
                    nestedSector.CeilingHeight = parentSector.CeilingHeight + defaultOffset;
                    break;

                default:
                    nestedSector.FloorHeight = parentSector.FloorHeight;
                    nestedSector.CeilingHeight = parentSector.CeilingHeight;
                    break;
            }
        }

        private void CreatePerimeterWalls(Sector sector)
        {
            // Add closing wall to complete the polygon
            sector.Walls.Add(new Wall
            {
                Start = sector.Vertices[sector.Vertices.Count - 1],
                End = sector.Vertices[0]
            });
        }

        private void CreateHeightTransitionWalls(Sector nestedSectorVertices, Sector nestedSector, Sector parentSector)
        {
            // Transfer the vertices to the nested sector
            nestedSector.Vertices.AddRange(nestedSectorVertices.Vertices);

            // Create walls for 2D visualization (so you can see the pit/platform outline)
            // These walls will be rendered differently in 3D as height transitions
            for (int i = 0; i < nestedSector.Vertices.Count; i++)
            {
                var startVertex = nestedSector.Vertices[i];
                var endVertex = nestedSector.Vertices[(i + 1) % nestedSector.Vertices.Count];

                nestedSector.Walls.Add(new Wall
                {
                    Start = startVertex,
                    End = endVertex
                });
            }

            Console.WriteLine(
                $"Created nested sector with {nestedSector.Walls.Count} outline walls - will render as height transitions in 3D");
        }

        private void UpdateParentSectorWithNestedTransitions(Sector parentSector)
        {
            // Find all nested sectors within this parent
            var nestedSectors = _sectors.Where(s => s.IsNested && s.ParentSectorId == parentSector.Id).ToList();

            // Clear any existing transition walls from parent
            // (Keep only the original perimeter walls)
            var originalWalls = parentSector.Walls.Take(parentSector.Vertices.Count).ToList();
            parentSector.Walls.Clear();
            parentSector.Walls.AddRange(originalWalls);

            // For each nested sector, add transition walls where heights differ
            foreach (var nestedSector in nestedSectors)
            {
                bool hasFloorDifference = Math.Abs(nestedSector.FloorHeight - parentSector.FloorHeight) > 0.1f;
                bool hasCeilingDifference = Math.Abs(nestedSector.CeilingHeight - parentSector.CeilingHeight) > 0.1f;

                if (hasFloorDifference || hasCeilingDifference)
                {
                    // Add transition walls around the nested area boundary
                    for (int i = 0; i < nestedSector.Vertices.Count; i++)
                    {
                        var startVertex = nestedSector.Vertices[i];
                        var endVertex = nestedSector.Vertices[(i + 1) % nestedSector.Vertices.Count];

                        var transitionWall = new Wall
                        {
                            Start = startVertex,
                            End = endVertex
                        };

                        parentSector.Walls.Add(transitionWall);
                    }

                    string wallType = "";
                    if (hasFloorDifference && !hasCeilingDifference)
                        wallType = nestedSector.FloorHeight < parentSector.FloorHeight ? "pit walls" : "platform walls";
                    else if (!hasFloorDifference && hasCeilingDifference)
                        wallType = nestedSector.CeilingHeight < parentSector.CeilingHeight
                            ? "lowered ceiling walls"
                            : "raised ceiling walls";
                    else
                        wallType = "full height transition walls";

                    Console.WriteLine(
                        $"Added {nestedSector.Vertices.Count} {wallType} to parent sector for nested area {nestedSector.Id}");
                }
            }
        }

        private void UpdateHeightTransitionWalls(Sector nestedSector, Sector parentSector)
        {
            // Nested sectors render their own walls as height transitions in 3D
            // No need to modify parent sector walls - this is handled by DrawNestedSectorHeightTransitionWall
            // Just force a visual refresh
        }

        private Color GetWallColorForSector(Sector sector)
        {
            if (!sector.IsNested)
                return Color.White; // Independent sectors use white walls

            // Nested sectors use cyan color
            return Color.Cyan;
        }
    }


    public class Camera2D
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; } = 1.0f;

        public Matrix GetViewMatrix()
        {
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
                   Matrix.CreateScale(Zoom, Zoom, 1);
        }

        public Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return Vector2.Transform(screenPos, Matrix.Invert(GetViewMatrix()));
        }

        public Rectangle GetViewBounds(Rectangle viewport)
        {
            var topLeft = ScreenToWorld(Vector2.Zero);
            var bottomRight = ScreenToWorld(new Vector2(viewport.Width, viewport.Height));

            return new Rectangle((int)topLeft.X, (int)topLeft.Y,
                (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));
        }
    }

    public enum SpriteAlignment
    {
        Floor, // Sprite always faces up (like floor decals, items on ground)
        Wall, // Sprite always faces horizontally (like wall decorations)
        Face // Sprite always faces the camera (billboarded)
    }

    public enum SpriteTag
    {
        Decoration, // Static decorative sprites
        Switch // Switch/button for activating doors/lifts - FUNCTIONAL
    }

    public class Sprite
    {
        public int Id { get; set; }
        public Vector2 Position { get; set; }
        public float Angle { get; set; } = 0f; // Rotation in degrees
        public Vector2 Scale { get; set; } = new Vector2(1f, 1f);
        public int Palette { get; set; } = 0; // For different color variations
        public SpriteAlignment Alignment { get; set; } = SpriteAlignment.Face;
        public SpriteTag Tag { get; set; } = SpriteTag.Decoration;
        public string TextureName { get; set; } = "Default";
        public float Height { get; set; } = 64f; // Height in world units
        public bool Visible { get; set; } = true;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        // Sector reference (sprites belong to sectors)
        public int SectorId { get; set; }

        // Tag system for behaviors and linking
        public int LoTag { get; set; } = 0; // Behavior tag (e.g., enemy type, pickup type, trigger type)
        public int HiTag { get; set; } = 0; // Link tag (links to other objects)

        public Sprite()
        {
            Id = Random.Shared.Next(1000, 9999);
        }
    }

    public class VertexHeight
    {
        public int VertexIndex { get; set; }
        public float FloorHeight { get; set; }
        public float CeilingHeight { get; set; }
    }
    
    // Build engine slope plane: z(x,y) = baseZ + (x-refX)*dx + (y-refY)*dy
    public class SlopePlane
    {
        public float BaseZ { get; set; } // Base height at reference point
        public Vector2 ReferencePoint { get; set; } // (xref, yref) - reference point 
        public float DeltaX { get; set; } // dx - height change per X unit
        public float DeltaY { get; set; } // dy - height change per Y unit
        
        public SlopePlane(float baseZ, Vector2 refPoint, float dx, float dy)
        {
            BaseZ = baseZ;
            ReferencePoint = refPoint;
            DeltaX = dx;
            DeltaY = dy;
        }
        
        // Evaluate plane equation at given position: z(x,y) = baseZ + (x-refX)*dx + (y-refY)*dy
        public float GetZAt(float x, float y)
        {
            float deltaX = x - ReferencePoint.X;
            float deltaY = y - ReferencePoint.Y;
            return BaseZ + deltaX * DeltaX + deltaY * DeltaY;
        }
    }

    public enum SectorType
    {
        Independent, // Completely separate room/area
        FloorPit, // Lowered floor within parent sector (divot/pit)
        FloorRaise, // Raised floor within parent sector (pillar/step)
        CeilingLower, // Lowered ceiling within parent sector (recessed alcove)
        CeilingRaise // Raised ceiling within parent sector (beam/protrusion)
    }

    public class Sector
    {
        public List<Vector2> Vertices { get; set; } = new List<Vector2>();
        public List<Wall> Walls { get; set; } = new List<Wall>();
        public List<Sprite> Sprites { get; set; } = new List<Sprite>();
        public int Id { get; set; }
        public float FloorHeight { get; set; } = 0f;
        public float CeilingHeight { get; set; } = 64f;

        // Build engine slope system - plane equations 
        public List<VertexHeight> VertexHeights { get; set; } = new List<VertexHeight>(); // Legacy vertex system
        public bool HasSlopes { get; set; } = false;
        
        // Proper Build engine slope planes: z(x,y) = floorz + (x-xref)*dx + (y-yref)*dy
        public SlopePlane? FloorPlane { get; set; } = null; // null = flat floor
        public SlopePlane? CeilingPlane { get; set; } = null; // null = flat ceiling

        // Nested sector system for Build engine style architecture
        public bool IsNested { get; set; } = false; // If true, this sector is inside another sector
        public int? ParentSectorId { get; set; } = null; // Parent sector this is nested within
        public SectorType SectorType { get; set; } = SectorType.Independent; // Type of sector
        public string FloorTexture { get; set; } = "Gray";
        public string CeilingTexture { get; set; } = "LightGray";
        public string WallTexture { get; set; } = "White";

        // Tag system for behaviors and linking
        public int LoTag { get; set; } = 0; // Behavior tag (e.g., door type, lift type)
        public int HiTag { get; set; } = 0; // Link tag (links to other objects)

        // Lift system properties
        public bool IsLift { get; set; } = false;
        public float LiftLowHeight { get; set; } = 0f; // Bottom position
        public float LiftHighHeight { get; set; } = 128f; // Top position
        public float LiftSpeed { get; set; } = 32f; // Units per second
        public LiftState LiftState { get; set; } = LiftState.AtBottom;
        public bool PlayerWasStandingOnLift { get; set; } = false; // Track if player was on lift when it started moving

        // Sector animation properties (Build engine style)
        public Vector2 AnimationOffset { get; set; } = Vector2.Zero; // For sliding doors
        public float AnimationRotation { get; set; } = 0f; // For rotating doors  
        public float AnimationHeightOffset { get; set; } = 0f; // For raising/lowering doors
        public Vector2 OriginalPosition { get; set; } = Vector2.Zero; // Original center position
        public bool IsAnimating { get; set; } = false;

        // UV Controls
        public bool FloorUVFlipX { get; set; } = false;
        public bool FloorUVFlipY { get; set; } = false;
        public bool CeilingUVFlipX { get; set; } = false;
        public bool CeilingUVFlipY { get; set; } = false;
        public bool WallUVFlipX { get; set; } = false;
        public bool WallUVFlipY { get; set; } = false;
        public float FloorUVOffsetX { get; set; } = 0f;
        public float FloorUVOffsetY { get; set; } = 0f;
        public float CeilingUVOffsetX { get; set; } = 0f;
        public float CeilingUVOffsetY { get; set; } = 0f;
        public float WallUVOffsetX { get; set; } = 0f;
        public float WallUVOffsetY { get; set; } = 0f;
        public float FloorUVRotation { get; set; } = 0f;
        public float CeilingUVRotation { get; set; } = 0f;
        public float WallUVRotation { get; set; } = 0f;
        public float FloorUVScaleX { get; set; } = 1f;
        public float FloorUVScaleY { get; set; } = 1f;
        public float CeilingUVScaleX { get; set; } = 1f;
        public float CeilingUVScaleY { get; set; } = 1f;
        public float WallUVScaleX { get; set; } = 1f;
        public float WallUVScaleY { get; set; } = 1f;

        // Shading
        public float FloorShading { get; set; } = 1f;
        public float CeilingShading { get; set; } = 1f;
        public float WallShading { get; set; } = 1f;

        public Sector(int id)
        {
            Id = id;
        }
    }

    public class Wall
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        public bool IsTwoSided { get; set; } = false;
        public int? AdjacentSectorId { get; set; }
    }

    public enum LiftState
    {
        AtBottom, // Lift is at lowest position
        Rising, // Lift is moving up  
        AtTop, // Lift is at highest position
        Lowering // Lift is moving down
    }

    // Level data container for save/load functionality
    public class LevelData
    {
        public string Name { get; set; } = "Untitled Level";
        public string Description { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";

        // Level geometry and gameplay data
        public List<Sector> Sectors { get; set; } = new List<Sector>();
        public Vector2? PlayerPosition { get; set; } = null;
        public bool HasPlayerPosition { get; set; } = false;

        // Editor state
        public int NextSectorId { get; set; } = 0;
        public int NextSpriteId { get; set; } = 0;

        // Level metadata
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }





   

   
    

    public enum EditMode
    {
        VertexPlacement,
        Selection,
        Delete,
        SpritePlace,
        SlopeEdit
    }
    
    // Extension methods for generating slope vertex heights
    public static class SectorExtensions
    {
        // Generate vertex heights for sloped sectors to enable proper collision detection  
        public static void GenerateSlopeVertexHeights(this Sector sector, float minFloorHeight, float maxFloorHeight, float minCeilingHeight, float maxCeilingHeight)
        {
            if (sector.Vertices.Count < 3) return;
            
            // Clear existing vertex heights
            sector.VertexHeights.Clear();
            
            // Create a gradient across the sector vertices based on Build engine vertical slice rendering
            var bounds = GetSectorBounds(sector);
            float width = bounds.Width;
            float height = bounds.Height;
            
            if (width < 1f || height < 1f) return;
            
            // Generate vertex heights for Build engine style vertical slices
            // This creates the height variations that collision detection needs
            for (int i = 0; i < sector.Vertices.Count; i++)
            {
                var vertex = sector.Vertices[i];
                
                // Calculate relative position within sector bounds (0.0 to 1.0)
                float relativeX = (vertex.X - bounds.X) / width;
                float relativeY = (vertex.Y - bounds.Y) / height;
                
                // Create height variation based on position for sloped surfaces
                // This simulates the vertical slice rendering that creates height changes
                float floorHeight = minFloorHeight + (maxFloorHeight - minFloorHeight) * relativeX;
                float ceilingHeight = minCeilingHeight + (maxCeilingHeight - minCeilingHeight) * relativeY;
                
                // Add some variation based on both X and Y for more realistic slopes
                floorHeight += (maxFloorHeight - minFloorHeight) * 0.2f * relativeY;
                ceilingHeight += (maxCeilingHeight - minCeilingHeight) * 0.2f * relativeX;
                
                var vertexHeight = new VertexHeight
                {
                    VertexIndex = i,
                    FloorHeight = floorHeight,
                    CeilingHeight = ceilingHeight
                };
                
                sector.VertexHeights.Add(vertexHeight);
                
                Console.WriteLine($"Generated vertex {i} heights: Floor={floorHeight:F1}, Ceiling={ceilingHeight:F1} at ({vertex.X:F1},{vertex.Y:F1})");
            }
            
            // Mark sector as having slopes for collision detection
            sector.HasSlopes = true;
            Console.WriteLine($"Sector {sector.Id} marked as sloped with {sector.VertexHeights.Count} vertex heights generated");
        }
        
        // Get bounding rectangle of sector vertices  
        private static System.Drawing.RectangleF GetSectorBounds(Sector sector)
        {
            if (sector.Vertices.Count == 0) return new System.Drawing.RectangleF(0, 0, 1, 1);
            
            float minX = sector.Vertices[0].X;
            float maxX = sector.Vertices[0].X;
            float minY = sector.Vertices[0].Y;
            float maxY = sector.Vertices[0].Y;
            
            foreach (var vertex in sector.Vertices)
            {
                minX = Math.Min(minX, vertex.X);
                maxX = Math.Max(maxX, vertex.X);
                minY = Math.Min(minY, vertex.Y);
                maxY = Math.Max(maxY, vertex.Y);
            }
            
            return new System.Drawing.RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
    }
}