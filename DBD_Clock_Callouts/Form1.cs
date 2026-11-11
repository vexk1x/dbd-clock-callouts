using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBD_Clock_Callouts
{
    public partial class Form1 : Form
    {

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


        private readonly Color _ForeColor = Color.White;
        private readonly Color _BackColor = Color.FromArgb(24, 24, 24);

        // img properties
        PictureBox pop_imgMap = null;
        private bool IsPoppedOut = false;
        private string map_name = "";
        private Image map_img = null;
        private Size img_size = new Size(250, 250);
        private bool img_ct = false;


        private bool DontSwallowWindow = false;


        private Version ver = new Version("0.0.1");

        Form Map;

        PictureBox SelectedMap = new PictureBox()
        {
            Size = new Size(200, 200),
            SizeMode = PictureBoxSizeMode.StretchImage
        };

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
            ResizeRedraw = false;

            FormBorderStyle = FormBorderStyle.None;
            BackColor = _BackColor;
            StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 250);

            Load += _Load;
            FormClosing += _Close;
        }

        private async void _Load(object sender, EventArgs e)
        {
            Panel Loading = new Panel()
            {
                Size = this.Size,
            };

            Label Status = new Label()
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "Loading...",
                Font = new Font("Seogue UI", 20f, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = _ForeColor
            };

            PictureBox Icon = new PictureBox()
            {
                Size = new Size(100, 100),
                Image = Properties.Resources.dbd_icon,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Location = new Point(140, 0)
            };

            Status.Controls.Add(Icon);
            Loading.Controls.Add(Status);
            Controls.Add(Loading);

            MapImages mapimages = new MapImages();

            List<string> Result = new List<string>();

            Result = await mapimages.LoadMapNames();

            Loading.Dispose();

            UI(Result, mapimages);
        }
        private void _Close(object sender, EventArgs e)
        {

        }

        private void RoundFCorners(int r)
        {
            GraphicsPath gp = new GraphicsPath();
            Rectangle rect = new Rectangle(0, 0, Width, Height);

            int d = r * 2;

            gp.StartFigure();

            gp.AddArc(rect.X, rect.Y, d, d, 180, 90);
            gp.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            gp.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            gp.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);

            gp.CloseFigure();

            Region = new Region(gp);

            gp.Dispose();
        }

        private void UI(List<string> names, MapImages mimages)
        {
            this.Size = new Size(500, 530);
            this.ClientSize = new Size(500, 530);

            RoundFCorners(8);

            this.Opacity = 0;

            Panel Titlebar = CreateTitlebar();

            ComboBox DropDown = CreateDropDown(names, mimages);

            Label Chose = new Label()
            {
                ForeColor = _ForeColor,
                Text = "Choose a Map: ",
                AutoSize = true,
            }; Chose.Location = new Point((DropDown.ClientSize.Width - Chose.ClientSize.Width) / 3, 43);

            Panel Seperator = CreateSeperator(70);

            SelectedMap.Location = new Point((ClientSize.Width - SelectedMap.ClientSize.Width) / 2, 71);

            DropDown.SelectedIndex = 0;

            Panel Seperator2 = CreateSeperator(291);

            CheckBox PopOutMap = new CheckBox()
            {
                Text = "View Map",
                Checked = false,
                ForeColor = _ForeColor,
                AutoSize = true
            }; PopOutMap.Location = new Point(20, Seperator2.Bottom + 5);


            CheckBox TopMostMap = new CheckBox()
            {
                Text = "Top Most",
                Checked = false,
                ForeColor = _ForeColor,
                AutoSize = true
            }; TopMostMap.Location = new Point(PopOutMap.Right + 15, Seperator2.Bottom + 5);

            CheckBox Clickthrough = new CheckBox()
            {
                Text = "Clickthroughable",
                Checked = false,
                ForeColor = _ForeColor,
                AutoSize = true
            }; Clickthrough.Location = new Point(TopMostMap.Right + 15, Seperator2.Bottom + 5);

            CheckBox PreventOutOfBounds = new CheckBox()
            {
                Text = "Prevent Map NoClip",
                Checked = false,
                ForeColor = _ForeColor,
                AutoSize = true,
            }; PreventOutOfBounds.Location = new Point(Clickthrough.Right + 15, Seperator2.Bottom + 5);

            PopOutMap.CheckedChanged += delegate
            {
                IsPoppedOut = PopOutMap.Checked;

                if (IsPoppedOut)
                {
                    if (Map == null || Map.IsDisposed)
                        this.PopOutMap();

                    if (img_ct)
                    {
                        SetClickthrough(img_ct);
                        img_ct = false;
                    }
                }
                else
                {
                    if (Map != null && !Map.IsDisposed)
                        Map.Close();

                    Map = null;
                    pop_imgMap = null;
                }
            };

            TopMostMap.CheckedChanged += delegate
            {
                if (Map == null || Map.IsDisposed)
                    return;

                Map.TopMost = TopMostMap.Checked;
            };

            Clickthrough.CheckedChanged += delegate
            {
                if (Map == null || Map.IsDisposed)
                {
                    img_ct = Clickthrough.Checked;
                    return;
                }

                SetClickthrough(Clickthrough.Checked);
            };

            PreventOutOfBounds.CheckedChanged += delegate
            {
                DontSwallowWindow = PreventOutOfBounds.Checked;
            };

            Panel Seperator3 = CreateSeperator(291 + 30);

            TrackBar ImageSizeX = new TrackBar()
            {
                Location = new Point(20, Seperator3.Bottom + 5),
                TickFrequency = 10,
                Minimum = 10,
                Maximum = 1000,
                TickStyle = TickStyle.None,
                Value = img_size.Width,
                Size = new Size(200, 50)
            };

            ImageSizeX.ValueChanged += delegate
            {
                img_size.Width = ImageSizeX.Value;

                if (pop_imgMap == null)
                    return;

                UpdImg();
            };

            Label limgx = new Label()
            {
                ForeColor = _ForeColor,
                AutoSize = true,
                Text = "Image Size X",
            }; limgx.Location = new Point(ImageSizeX.Left + (ImageSizeX.ClientSize.Width - limgx.ClientSize.Width) / 2 + 5, ImageSizeX.Bottom);

            TrackBar ImageSizeY = new TrackBar()
            {
                TickFrequency = 10,
                Minimum = 10,
                Maximum = 1000,
                TickStyle = TickStyle.None,
                Value = img_size.Height,
                Size = new Size(200, 50),
                Location = new Point(ImageSizeX.Right + 50, Seperator3.Bottom + 5),
            };

            ImageSizeY.ValueChanged += delegate
            {
                img_size.Height = ImageSizeY.Value;

                if (pop_imgMap == null)
                    return;

                UpdImg();
            };

            Label limgy = new Label()
            {
                ForeColor = _ForeColor,
                AutoSize = true,
                Text = "Image Size Y",
            }; limgy.Location = new Point(ImageSizeY.Left + (ImageSizeY.ClientSize.Width - limgy.ClientSize.Width) / 2 + 5, ImageSizeY.Bottom);

            Label ResetSize = new Label()
            {
                ForeColor = _ForeColor,
                Text = "Reset Window Size",
                AutoSize = true,
                Font = new Font("Segoue UI", Font.Size, FontStyle.Underline, GraphicsUnit.Point),
                Location = new Point(limgx.Width - limgy.Width, limgy.Bottom + 15)
            };

            ResetSize.MouseClick += delegate
            {
                img_size = new Size(250, 250);
                ImageSizeY.Value = 250;
                ImageSizeX.Value = 250;

                UpdImg();
            };

            Panel Seperator4 = CreateSeperator(450);

            TrackBar TransparentMap = new TrackBar()
            {
                TickFrequency = 1,
                Minimum = 1,
                Maximum = 100,
                TickStyle = TickStyle.None,
                Value = 100,
                Size = new Size(200, 50),
                Location = new Point(20, Seperator4.Bottom + 5),
            };

            TransparentMap.ValueChanged += delegate
            {
                if (Map == null || Map.IsDisposed)
                    return;

                Map.Opacity = (TransparentMap.Value / 100.0);
            };

            Label ltransparentmap = new Label()
            {
                ForeColor = _ForeColor,
                AutoSize = true,
                Text = "Map Transparency",
            }; ltransparentmap.Location = new Point(TransparentMap.Left + (TransparentMap.ClientSize.Width - ltransparentmap.ClientSize.Width) / 2 + 5, TransparentMap.Bottom);

            Controls.AddRange(new Control[]
            {
                Titlebar,
                DropDown,
                Chose,
                Seperator, 
                SelectedMap,
                Seperator2,
                PopOutMap,
                TopMostMap,
                Seperator3,
                ImageSizeX,
                ImageSizeY,
                limgx,
                limgy,
                Clickthrough,
                PreventOutOfBounds,
                ResetSize,
                Seperator4,
                TransparentMap,
                ltransparentmap
            });

            Opacity = 0.99;
        }

        private Panel CreateTitlebar()
        {
            Panel Titlebar = new Panel()
            {
                Dock = DockStyle.Top,
                Width = ClientSize.Width,
                Height = 36,
                BackColor = _BackColor
            };

            Panel Seperator = new Panel()
            {
                BackColor = Color.Gray,
                Height = 1,
                Location = new Point(0, Titlebar.Height - 1),
                Dock = DockStyle.Bottom
            };

            Button ExitButton = new Button()
            {
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Red, MouseDownBackColor = Color.Red },
                Size = new Size(40, Titlebar.Height - 1),
                ForeColor = _ForeColor,
                Text = "\u2715",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            Button MinimizeButton = new Button()
            {
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(160, 160, 160), MouseDownBackColor = Color.FromArgb(160, 160, 160) },
                Size = new Size(40, Titlebar.Height - 1),
                ForeColor = _ForeColor,
                Text = "\u0C7C",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            ExitButton.Location = new Point(Titlebar.Width - 40, 0);
            MinimizeButton.Location = new Point(Titlebar.Width - 80, 0);

            ExitButton.Click += (sender, e) => Close();
            MinimizeButton.Click += (sender, e) => WindowState = FormWindowState.Minimized;

            PictureBox Icon = new PictureBox()
            {
                Size = new Size(20, 20),
                Image = Properties.Resources.dbd_icon,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Location = new Point(9, 6)
            };

            Label yesyes = new Label()
            {
                Text = $"DBD Clock Callouts v{ver} | vexk1x",
                AutoSize = true,
                ForeColor = _ForeColor,
                TextAlign = ContentAlignment.MiddleCenter
            }; yesyes.Location = new Point((Icon.Right + yesyes.ClientSize.Width) / 2 - 20, 10);

            yesyes.MouseDown += Drag;

            Titlebar.Controls.AddRange(new Control[] 
            {
                Seperator,
                MinimizeButton, 
                ExitButton, 
                Icon,
                yesyes
            });

            Titlebar.MouseDown += Drag;

            return Titlebar;
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
            }
        }

        private ComboBox CreateDropDown(List<string> names, MapImages mimages)
        {
            ComboBox DropDown = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = _BackColor,
                ForeColor = _ForeColor,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Size = new Size(250, 40),
                FlatStyle = FlatStyle.Flat,
                AutoCompleteMode = AutoCompleteMode.Suggest,
            };

            DropDown.Location = new Point((ClientSize.Width - DropDown.ClientSize.Width) / 2, 40);

            DropDown.DrawItem += (sender, e) =>
            {
                if (e.Index < 0)
                    return;

                bool hover = (e.State & DrawItemState.Selected) == DrawItemState.Selected && (e.State & DrawItemState.ComboBoxEdit) != DrawItemState.ComboBoxEdit;

                Color c = hover ? Color.DimGray : _BackColor;

                SolidBrush sbrush = new SolidBrush(c);

                e.Graphics.FillRectangle(sbrush, e.Bounds);

                TextRenderer.DrawText(e.Graphics, DropDown.Items[e.Index].ToString(), e.Font, e.Bounds, _ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                sbrush.Dispose();
            };

            DropDown.Items.AddRange(names.ToArray());
            DropDown.SelectedIndexChanged += (sender, e) => { OnDropDownSelectedIndexChanged(sender, e, mimages, DropDown.SelectedIndex, names); };

            return DropDown;
        }

        private async void OnDropDownSelectedIndexChanged(object sender, EventArgs e, MapImages mimages, int index, List<string> names)
        {
            try
            {
                if (index < 0 || index >= names.Count)
                    return;

                Image map = await mimages.GetImage(names[index]);

                SelectedMap.Image = map;

                map_img = map;
                map_name = names[index];

                if (Map == null || pop_imgMap == null)
                    return;

                UpdImg();
            }
            catch
            {
                throw new Exception("Couldn't Load Image");
            }
        }

        private Panel CreateSeperator(int y)
        {
            return new Panel()
            {
                Width = ClientSize.Width,
                Height = 1,
                BackColor = Color.Gray,
                Location = new Point(0, y),
            };
        }

        private void PopOutMap()
        {
            if (!IsPoppedOut || map_img == null)
                return;

            Map = new Form()
            {
                Text = map_name,
                ClientSize = img_size,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.None,
                TopMost = false,
                Size = img_size,
                Icon = this.Icon
            };

            Map.ClientSize = img_size;

            pop_imgMap = new PictureBox()
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = map_img,
                Dock = DockStyle.Fill,
            };

            pop_imgMap.MouseDown += MapDrag;
            Map.MouseDown += MapDrag;

            Map.Controls.Add(pop_imgMap);
            Map.Show();
        }

        private void UpdImg()
        {
            try
            {
                pop_imgMap.Image = map_img;
                pop_imgMap.Size = img_size;

                Map.Size = img_size;
                Map.ClientSize = img_size;
                Map.Text = map_name;
            }
            catch (NullReferenceException) // If you reset while map is not shown, it throws
            {
                return;
            };
        }

        private void MapDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (DontSwallowWindow)
                    PreventOutOfBounds();
                

                ReleaseCapture();
                SendMessage(Map.Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
            }
        }

        private void PreventOutOfBounds() // prevents the map from being swallowed
        {
            if (Map.Location.X < 0)
            {
                Map.Left += 25;
            }

            if (Map.Location.Y < 0)
            {
                Map.Top -= 25;
            }
        }

        private void SetClickthrough(bool yesno)
        {
            if (Map == null || Map.IsDisposed)
                return;

            IntPtr h = Map.Handle;

            int GWL_EXSTYLE = -20;
            int WS_EX_LAYERED = 0x80000;
            int WS_EX_TRANSPARENT = 0x20;

            var style = yesno ? GetWindowLong(h, GWL_EXSTYLE) | WS_EX_LAYERED | WS_EX_TRANSPARENT : (GetWindowLong(h, GWL_EXSTYLE) | WS_EX_LAYERED) ^ WS_EX_TRANSPARENT;

            SetWindowLong(h, GWL_EXSTYLE, style);
        }
    }
}
