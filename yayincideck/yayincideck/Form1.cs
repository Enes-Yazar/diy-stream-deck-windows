using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO;
using static yayincideck.Form1;
using QRCoder;
using System.Xml;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using NAudio.CoreAudioApi;
using AudioSwitcher.AudioApi.CoreAudio;

namespace yayincideck
{

    public partial class Form1 : Form
    {

        private DeckButtonInfo deckjson;

        private List<Button> buttons;
        private Dictionary<Button, string> buttonTexts = new Dictionary<Button, string>();
        private Dictionary<Button, Font> buttonFonts = new Dictionary<Button, Font>();
        private Dictionary<Button, Color> buttonForeColors = new Dictionary<Button, Color>();
        private Dictionary<Button, ContentAlignment> buttonTextAlignments = new Dictionary<Button, ContentAlignment>();
        private Dictionary<Button, List<ButtonAction>> buttonActionOptions = new Dictionary<Button, List<ButtonAction>>();
        public Dictionary<Button, ButtonAction> buttonActions = new Dictionary<Button, ButtonAction>();
        public Dictionary<Button, Image> buttonImageMap = new Dictionary<Button, Image>();
        public List<Button> droppedButtons = new List<Button>();
        private Button selectedButton = null;
        string filePath = @"D:\Yazilim\VisualStudio Projeleri\yayincideck\buttons.json";

        public Form1()
        {

            deckjson = new DeckButtonInfo(this);

            InitializeComponent();
            buttons = new List<Button> { button1, button2, button3, button8, button9, button11, button12, button13, button14, button16 };
            MakeButtonRounded(buttonseskontrol, 10);
            MakeButtonRounded(navigasyon, 10);
            MakeButtonRounded(sistemayarı, 10);
            this.Load += Form1_Load;

            comboBox1.DataSource = Enum.GetValues(typeof(ButtonAction));

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Programs.Start();

            Task.Run(() => StartHttpListener());

            if (!File.Exists(filePath))
            {
                // Eğer dosya yoksa, boş bir JSON yapısı ile oluştur
                var emptyData = new List<DeckButtonInfo>(); // veya varsayılan veri ile dolu bir liste
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(emptyData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);
            }


            RegisterPanelEvents();
            foreach (Button button in buttons)
            {
                MakeButtonRounded(button, 10);
                button.MouseDown += CommandButton_MouseDown;
            }

            Dictionary<Button, Image> buttonImageMap = new Dictionary<Button, Image>();
            AddButtonImage(buttonImageMap, button1, Properties.Resources.streamdeck_key);
            AddButtonImage(buttonImageMap, button2, Properties.Resources.acikses);
            AddButtonImage(buttonImageMap, button3, Properties.Resources.volumekis);
            AddButtonImage(buttonImageMap, button8, Properties.Resources.microfadacik);
            AddButtonImage(buttonImageMap, button9, Properties.Resources.kulaklik);
            AddButtonImage(buttonImageMap, button12, Properties.Resources.nextpage);
            AddButtonImage(buttonImageMap, button14, Properties.Resources.backpage);
            AddButtonImage(buttonImageMap, button13, Properties.Resources.file);
            AddButtonImage(buttonImageMap, button11, Properties.Resources.openfl);
            AddButtonImage(buttonImageMap, button16, Properties.Resources.exit);

            List<Panel> targetPanels = new List<Panel> { sesac, sayfalararasi, sistempaneli };

            // Assign background images to buttons
            foreach (Button button in buttons)
            {
                bool isInTargetPanel = targetPanels.Any(panel => panel.Controls.Contains(button));

                if (isInTargetPanel)
                {
                    button.BackgroundImage = null; 
                }
                else
                {
                    button.BackgroundImage = buttonImageMap.ContainsKey(button) ? buttonImageMap[button] : null;
                    button.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }

            foreach (FontFamily font in FontFamily.Families)
            {
                comboBox2.Items.Add(font.Name);
            }

            buttonActionOptions[button1] = new List<ButtonAction> { ButtonAction.MuteFull, ButtonAction.Stop, ButtonAction.IncreaseVolume, ButtonAction.DecreaseVolume };
            buttonActionOptions[button2] = new List<ButtonAction> { ButtonAction.VolumeUp, ButtonAction.Stop, ButtonAction.DecreaseVolume };
            buttonActionOptions[button3] = new List<ButtonAction> { ButtonAction.VolumeDown, ButtonAction.Stop, ButtonAction.DecreaseVolume };
            buttonActionOptions[button8] = new List<ButtonAction> { ButtonAction.MuteMic,ButtonAction.UnmuteMicrophone};
            buttonActionOptions[button9] = new List<ButtonAction> { ButtonAction.MuteKulaklik };
            buttonActionOptions[button11] = new List<ButtonAction> { ButtonAction.OpenApp, ButtonAction.CloseApp };
            buttonActionOptions[button12] = new List<ButtonAction> { ButtonAction.NextPage, ButtonAction.GoToPage, ButtonAction.BackPage };
            buttonActionOptions[button13] = new List<ButtonAction> { ButtonAction.GoToPage, ButtonAction.BackPage, ButtonAction.NextPage };
            buttonActionOptions[button14] = new List<ButtonAction> { ButtonAction.BackPage, ButtonAction.NextPage, ButtonAction.GoToPage };
            buttonActionOptions[button16] = new List<ButtonAction> { ButtonAction.CloseApp, ButtonAction.OpenApp };


            GenerateAndSetQRCode();  // AW 

        }



        private void RegisterPanelEvents()
        {
            List<Panel> panels = new List<Panel>
            {
        panel1, panel4, panel5, panel6, panel7, panel8, panel9, panel10,
        panel11, panel12, panel13, panel14, panel15, panel16, panel17
            };

            foreach (var panel in panels)
            {
                panel.AllowDrop = true;
                panel.DragEnter += Panel_DragEnter;
                panel.DragDrop += Panel_DragDrop;
                panel.Paint += panel_Paint;
            }
        }
        private void AddButtonImage(Dictionary<Button, Image> map, Button button, byte[] imageBytes)
        {
            if (imageBytes != null)
            {
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    map.Add(button, Image.FromStream(ms));
                }
            }
            else
            {
                map.Add(button, null); //  null 
            }
        }

        private void panel_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);
        }
        private void MakeButtonRounded(Button button, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            Rectangle rect = new Rectangle(0, 0, button.Width, button.Height);
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Width - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Width - diameter, rect.Height - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            button.Region = new Region(path);
        }
        private void CommandButton_MouseDown(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            DoDragDrop(btn, DragDropEffects.Copy);
        }

        private void Panel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }


        private void Panel_DragDrop(object sender, DragEventArgs e)
        {
            Panel panel = sender as Panel; // Sender panel
            if (panel == null) return;

            if (e.Data.GetDataPresent(typeof(Button)))
            {
                Button originalButton = (Button)e.Data.GetData(typeof(Button));

                Button newButton = new Button
                {
                    Text = "",
                    Size = originalButton.Size,
                    BackColor = originalButton.BackColor,
                    ForeColor = originalButton.ForeColor,
                    Margin = originalButton.Margin,
                    FlatStyle = originalButton.FlatStyle,
                    BackgroundImageLayout = ImageLayout.Zoom,
                    Image = originalButton.Image,
                    Font = originalButton.Font,
                    Padding = originalButton.Padding,
                    Cursor = originalButton.Cursor,
                    TextAlign = originalButton.TextAlign,
                    UseVisualStyleBackColor = originalButton.UseVisualStyleBackColor,
                };

                Dictionary<Button, Image> buttonImageMap = new Dictionary<Button, Image>();

                AddButtonImage(buttonImageMap, button1, Properties.Resources.streamdeck_key);
                AddButtonImage(buttonImageMap, button2, Properties.Resources.acikses);
                AddButtonImage(buttonImageMap, button3, Properties.Resources.volumekis);
                AddButtonImage(buttonImageMap, button8, Properties.Resources.microfadacik);
                AddButtonImage(buttonImageMap, button9, Properties.Resources.kulaklik);
                AddButtonImage(buttonImageMap, button12, Properties.Resources.nextpage);
                AddButtonImage(buttonImageMap, button14, Properties.Resources.backpage);
                AddButtonImage(buttonImageMap, button13, Properties.Resources.file);
                AddButtonImage(buttonImageMap, button11, Properties.Resources.openfl);
                AddButtonImage(buttonImageMap, button16, Properties.Resources.exit);


                newButton.Location = new Point(0, 0);
                newButton.Size = panel.Size;
                newButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                newButton.FlatStyle = FlatStyle.Flat;
                newButton.FlatAppearance.BorderSize = 0;
                newButton.BackColor = ColorTranslator.FromHtml("#2E2E2E");

                List<Panel> noBackgroundPanels = new List<Panel> { sesac, sayfalararasi, sistempaneli };

                if (!noBackgroundPanels.Contains(panel))
                {
                    byte[] imageBytes = Properties.Resources.streamdeck_key;
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        Image backgroundImage = Image.FromStream(ms);
                        newButton.BackgroundImage = buttonImageMap[originalButton];
                        newButton.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                }
                else
                {
                    newButton.BackgroundImage = null;
                }

                newButton.FlatAppearance.BorderColor = originalButton.FlatAppearance.BorderColor;
                newButton.FlatAppearance.MouseOverBackColor = originalButton.FlatAppearance.MouseOverBackColor;
                newButton.FlatAppearance.MouseDownBackColor = originalButton.FlatAppearance.MouseDownBackColor;
                newButton.FlatAppearance.CheckedBackColor = originalButton.FlatAppearance.CheckedBackColor;

                droppedButtons.Add(newButton);

                newButton.Click += (s, args) =>
                {
                    selectedButton = (Button)s;

                    // Disable the click event if the button is in panel2
                    if (panel == panel2)
                    {
                        MessageBox.Show("This button cannot be clicked on this panel!");
                    }
                    else
                    {

                        if (buttonActionOptions.ContainsKey(originalButton))
                        {
                            comboBox1.DataSource = buttonActionOptions[originalButton];
                        }
                        else
                        {
                            comboBox1.DataSource = new List<ButtonAction>();
                        }


                        UpdateButtonSettings();

                        if (originalButton == button1)
                        {
                            label1.Text = "System : Multimedia";
                            ExecuteButtonAction(selectedButton);


                            byte[] imageBytes = Properties.Resources.streamdeck_key;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            label7.Visible = false;
                            comboBoxmikrofon.Visible = false;

                        }
                        else if (originalButton == button2)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "System : Multimedia";
                            byte[] imageBytes = Properties.Resources.acikses;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            label7.Visible = false;
                            comboBoxmikrofon.Visible = false;
                        }
                        else if (originalButton == button3)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "System : Multimedia";
                            byte[] imageBytes = Properties.Resources.volumekis;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            label7.Visible = false;
                            comboBoxmikrofon.Visible = false;
                        }
                        else if (originalButton == button8)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "Volume Controller : Input Device Control";

                            byte[] imageBytes = Properties.Resources.microfadacik;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            comboBoxmikrofon.Items.Clear();
                            label7.Visible = true;
                            comboBoxmikrofon.Visible =true;

                            for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
                            {
                                var deviceInfo = NAudio.Wave.WaveIn.GetCapabilities(i);
                                comboBoxmikrofon.Items.Add($"{i}: {deviceInfo.ProductName}");
                            }

                            if (comboBoxmikrofon.Items.Count > 0)
                            {
                                comboBoxmikrofon.SelectedIndex = 0; // Varsayılan olarak ilkini seç
                            }

                        }
                        else if (originalButton == button9)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "Volume Controller : Output Device Control";

                            byte[] imageBytes = Properties.Resources.kulaklik;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            comboBoxmikrofon.Items.Clear();
                            label7.Text = "Headpeons";
                            label7.Visible = true;
                            comboBoxmikrofon.Visible = true;


                            var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                            var devices = enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.DeviceState.Active);

                            for (int i = 0; i < devices.Count; i++)
                            {
                                comboBoxmikrofon.Items.Add($"{i}: {devices[i].FriendlyName}");
                            }

                            if (comboBoxmikrofon.Items.Count > 0)
                            {
                                comboBoxmikrofon.SelectedIndex = 0;
                            }

                        }
                        else if (originalButton == button12)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "Next Page";

                            byte[] imageBytes = Properties.Resources.nextpage;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            label7.Visible = false;
                            comboBoxmikrofon.Visible = false;
                        }
                        else if (originalButton == button13)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "Go Page";

                            byte[] imageBytes = Properties.Resources.file;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            label7.Visible = false;
                            comboBoxmikrofon.Visible = false;
                        }
                        else if (originalButton == button14)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "Back Page";
                            byte[] imageBytes = Properties.Resources.backpage;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            label7.Visible = false;
                            comboBoxmikrofon.Visible = false;
                        }
                        else if (originalButton == button11)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "System : Open Aplication";

                            byte[] imageBytes = Properties.Resources.openfl;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            label7.Visible = false;
                            comboBoxmikrofon.Visible = false;
                        }
                        else if (originalButton == button16)
                        {
                            ExecuteButtonAction(selectedButton);

                            label1.Text = "System : Close";
                            byte[] imageBytes = Properties.Resources.exit;
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image image = Image.FromStream(ms);
                                button55.BackgroundImage = image;
                                button55.BackgroundImageLayout = ImageLayout.Stretch; // isteğe bağlı
                            }
                            label7.Visible = false;
                            comboBoxmikrofon.Visible = false;
                        }

                        panel3.Visible = true;
                        panel56.Visible = false;

                    }
                };

                panel.Controls.Add(newButton);
                MakeButtonRounded(newButton, 10);
                deckjson.SaveDroppedButtonsToJson(filePath);

            }
        }


        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panellers(object sender, PaintEventArgs e)
        {
            // Panelin kenarlığını önce çiziyoruz
            Panel panel = sender as Panel;
            if (panel == null) return;

            int radius = 40; // Kenar yuvarlama çapı
            Color borderColor = ColorTranslator.FromHtml("#474747"); // Kenar rengi
            int borderWidth = 2; // Kenar genişliği

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Çizim kalitesini artırır

            // Kenar çizim alanı
            Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();

                // Kenar çizimi
                using (Pen pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }
        }


        private void panel6_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);
        }

        private void panel7_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel8_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel10_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel11_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel9_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel12_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel13_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel14_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }


        private void Form1_Load_1(object sender, EventArgs e)
        {
            panel3.Visible = false;
            UpdateLayout();

            //-17; 0
            //-23; 42
        }

        private void panel15_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel16_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel17_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void panel56_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripComboBox1_Click_1(object sender, EventArgs e)
        {

        }

        private void buttonseskontrol_Click(object sender, EventArgs e)
        {
            sesac.Visible = !sesac.Visible;

            UpdateLayout();

        }
        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            panellers(sender, e);

        }

        private void navigasyon_Click(object sender, EventArgs e)
        {
            sayfalararasi.Visible = !sayfalararasi.Visible;
            UpdateLayout();

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }


        private void sayfalararasi_Paint(object sender, PaintEventArgs e)
        {

        }

        private void sisbuton_Paint(object sender, PaintEventArgs e)
        {

        }

        private void sistemayarı_Click(object sender, EventArgs e)
        {
            sistempaneli.Visible = !sistempaneli.Visible;
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            int leftMargin = 7;
            int yOffset = 12;
            int spacing = 6;   

            buttonseskontrol.Location = new Point(leftMargin, yOffset);
            yOffset += buttonseskontrol.Height + spacing;

            //  paneli
            if (sesac.Visible)
            {
                sesac.Location = new Point(leftMargin + 18, yOffset); 
                yOffset += sesac.Height + spacing;
            }

            // 2. Navigasyon Butonu
            navigasyon.Location = new Point(leftMargin, yOffset);
            yOffset += navigasyon.Height + spacing;

            // 2.1 Sayfalararası panel
            if (sayfalararasi.Visible)
            {
                sayfalararasi.Location = new Point(leftMargin + 18, yOffset);
                yOffset += sayfalararasi.Height + spacing;
            }

            sistemayarı.Location = new Point(leftMargin, yOffset);
            yOffset += sistemayarı.Height + spacing;

            // 3.1 system panel
            if (sistempaneli.Visible)
            {
                sistempaneli.Location = new Point(leftMargin + 18, yOffset);
                yOffset += sistempaneli.Height + spacing;
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            if (selectedButton != null)
            {
                Control parent = selectedButton.Parent;
                if (parent != null)
                {
                    parent.Controls.Remove(selectedButton);
                    buttonTexts.Remove(selectedButton);
                    buttonFonts.Remove(selectedButton);
                    buttonForeColors.Remove(selectedButton);
                    buttonTextAlignments.Remove(selectedButton);
                    droppedButtons.Remove(selectedButton);
                    selectedButton.Dispose();
                    selectedButton = null;
                    panel3.Visible = false;
                    panel56.Visible = true;
                    textBox1.Text = "";
                    deckjson.SaveDroppedButtonsToJson(filePath);

                }
            }
            else
            {
                MessageBox.Show("Please select the button you want to delete first.");
            }
        }
        private void UpdateButtonSettings()
        {
            if (selectedButton == null)
            {
                textBox1.Text = "";
                listBox1.SelectedIndex = -1;
                comboBox2.SelectedIndex = -1;
                return;
            }

            // Metin
            textBox1.Text = buttonTexts.ContainsKey(selectedButton) ? buttonTexts[selectedButton] : selectedButton.Text;

            // Font Boyutu
            if (buttonFonts.ContainsKey(selectedButton))
            {
                float fontSize = buttonFonts[selectedButton].Size;
                listBox1.SelectedItem = fontSize.ToString();
            }
            else
            {
                listBox1.SelectedIndex = -1;
            }

            // Font Tipi
            if (buttonFonts.ContainsKey(selectedButton))
            {
                string fontName = buttonFonts[selectedButton].FontFamily.Name;
                comboBox2.SelectedItem = fontName;
            }
            else
            {
                comboBox2.SelectedIndex = -1;
            }

            if (buttonTextAlignments.ContainsKey(selectedButton))
            {
                ContentAlignment alignment = buttonTextAlignments[selectedButton];

            }

            textBox1.Focus();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (selectedButton != null)
            {
                selectedButton.Text = textBox1.Text;
                buttonTexts[selectedButton] = textBox1.Text;
                deckjson.SaveDroppedButtonsToJson(filePath);

            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count == 0) return;

            int nextIndex = listBox1.SelectedIndex + 1;
            if (nextIndex >= listBox1.Items.Count)
                nextIndex = 0; // Başa dön

            listBox1.SelectedIndex = nextIndex;
            deckjson.SaveDroppedButtonsToJson(filePath);

        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count == 0) return;

            int prevIndex = listBox1.SelectedIndex - 1;
            if (prevIndex < 0)
                prevIndex = listBox1.Items.Count - 1; // Sona dön

            listBox1.SelectedIndex = prevIndex;
            deckjson.SaveDroppedButtonsToJson(filePath);

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.SelectionMode = SelectionMode.One;

            if (selectedButton != null && listBox1.SelectedItem != null)
            {
                if (int.TryParse(listBox1.SelectedItem.ToString(), out int newFontSize))
                {
                    Font currentFont = buttonFonts.ContainsKey(selectedButton) ? buttonFonts[selectedButton] : selectedButton.Font;
                    Font newFont = new Font(currentFont.FontFamily, newFontSize, currentFont.Style);
                    selectedButton.Font = newFont;
                    buttonFonts[selectedButton] = newFont;
                }
            }
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            Color bgColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? Color.White 
                : listBox1.BackColor;

            using (SolidBrush backgroundBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            }

            string text = listBox1.Items[e.Index].ToString();
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            {
                e.Graphics.DrawString(text, e.Font, textBrush, e.Bounds);
            }

            e.DrawFocusRectangle();
        }

        private void comboBox2_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            string fontName = comboBox2.Items[e.Index].ToString();
            Color bgColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? Color.LightGray
                : comboBox2.BackColor;

            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            using (Font font = new Font(fontName, 12))
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            {
                e.Graphics.DrawString(fontName, font, textBrush, e.Bounds);
            }

            e.DrawFocusRectangle();

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selectedButton != null && comboBox2.SelectedItem != null)
            {
                string selectedFontName = comboBox2.SelectedItem.ToString();
                float currentSize = buttonFonts.ContainsKey(selectedButton) ? buttonFonts[selectedButton].Size : selectedButton.Font.Size;
                Font newFont = new Font(selectedFontName, currentSize);
                selectedButton.Font = newFont;
                buttonFonts[selectedButton] = newFont;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (selectedButton == null)
            {
                MessageBox.Show("First, select a button.");
                return;
            }

            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedButton.ForeColor = colorDialog.Color;
                    buttonForeColors[selectedButton] = colorDialog.Color;
                    deckjson.SaveDroppedButtonsToJson(filePath);

                }
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (selectedButton != null)
            {
                selectedButton.TextAlign = ContentAlignment.MiddleCenter;
                buttonTextAlignments[selectedButton] = ContentAlignment.MiddleCenter;
                deckjson.SaveDroppedButtonsToJson(filePath);

            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (selectedButton != null)
            {
                selectedButton.TextAlign = ContentAlignment.TopCenter;
                buttonTextAlignments[selectedButton] = ContentAlignment.TopCenter;
                deckjson.SaveDroppedButtonsToJson(filePath);

            }

        }

        private void button18_Click(object sender, EventArgs e)
        {
            if (selectedButton != null)
            {
                selectedButton.TextAlign = ContentAlignment.BottomCenter;
                buttonTextAlignments[selectedButton] = ContentAlignment.BottomCenter;
                deckjson.SaveDroppedButtonsToJson(filePath);

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {

        }
        private void PopulateComboBox1()
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(Enum.GetNames(typeof(ButtonAction)));
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selectedButton != null && comboBox1.SelectedItem != null)
            {
                string selectedAction = comboBox1.SelectedItem.ToString();
                if (Enum.TryParse(selectedAction, out ButtonAction action))
                {
                    buttonActions[selectedButton] = action;
                    deckjson.SaveDroppedButtonsToJson(filePath);

                }
            }

        }

        private void ExecuteButtonAction(Button button)
        {
            if (buttonActions.ContainsKey(button))
            {
                switch (buttonActions[button])
                {
                    case ButtonAction.VolumeUp:
                        VolumeManager.IncreaseVolume();
                        break;
                    case ButtonAction.VolumeDown:
                        VolumeManager.DecreaseVolume();
                        break;
                    case ButtonAction.MuteMic:
                      //  VolumeManager.MuteMicrophone();
                        break;
                    case ButtonAction.UnmuteMicrophone:
                        VolumeManager.UnmuteMicrophone();
                        break;
                    case ButtonAction.MuteKulaklik:
                        VolumeManager.MuteHeadphones();
                        break;
                    case ButtonAction.MuteHeadphones:
                        VolumeManager.UnmuteHeadphones();
                        break;
                    case ButtonAction.CloseApp:
                        this.Close();
                        break;
                }
            }
        }


        public enum ButtonAction
        {
            None,
            MuteFull,
            MuteKulaklik,
            OpenApp,
            VolumeUp,
            VolumeDown,
            MuteMicropone,
            UnmuteMicrophone,
            NextPage,
            MuteHeadphones,
            GoToPage,
            MuteMic,
            BackPage,
            CloseApp,
            Stop,
            IncreaseVolume,
            DecreaseVolume,
        }

        private void button20_Click(object sender, EventArgs e)
        {

        }

        private void GenerateAndSetQRCode()
        {
            string ipAddress = GetLocalIPAddress();
            string url = $"http://{ipAddress}:5001/buttons.json";

            GenerateQRCode(url);

        }
        private void GenerateQRCode(string text)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);

            button20.BackgroundImage = qrCodeImage;
            button20.BackgroundImageLayout = ImageLayout.Stretch; // Daha güzel görünür

        }
        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        private void button21_Click(object sender, EventArgs e)
        {
            panel18.Visible = true;
        }

        private void button22_Click(object sender, EventArgs e)
        {
            panel18.Visible = false;

        }

        private async Task StartHttpListener()
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://+:5001/");
            httpListener.Start();

            while (true)
            {
                var context = await httpListener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // 1. buttons.json isteği kontrolü
                if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/buttons.json")
                {
                    try
                    {
                        string json = File.ReadAllText(@"D:\Yazilim\VisualStudio Projeleri\yayincideck\buttons.json");
                        byte[] buffer = Encoding.UTF8.GetBytes(json);
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("buttons.json unreadable: " + ex.Message);
                        response.StatusCode = 500;
                    }
                    response.OutputStream.Close();
                    continue; // işlem bitti, döngü başa dönsün
                }

                // 2. Komut gönderimi kontrolü (örneğin ?command=VolumeUp)
                string command = request.QueryString["command"];
                if (!string.IsNullOrEmpty(command))
                {
                    switch (command.ToLower())
                    {
                        case "volumeup":
                        case "increasevolume":
                            VolumeManager.IncreaseVolume();
                            break;
                        case "volumedown":
                        case "decreasevolume":
                            VolumeManager.DecreaseVolume();
                            break;
                        case "mutemic":
                            VolumeManager.MuteVolume();
                            break;
                        case "unmutemic":
                            VolumeManager.UnmuteMicrophone();
                            break;
                        case "MuteHeadphones":
                            VolumeManager.MuteHeadphones();
                            break;
                        case "UnmuteHeadphones":
                            VolumeManager.UnmuteHeadphones();
                            break;
                        case "closeapp":
                            this.Invoke((Action)(() => this.Close()));
                            break;
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes("OK");
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else
                {
                    response.StatusCode = 400;
                    response.OutputStream.Close();
                }
            }
        }

      
    }

    public static class VolumeManager
    {
        public static void IncreaseVolume()
        {
            var defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            defaultPlaybackDevice.Volume = Math.Min(defaultPlaybackDevice.Volume + 5, 100);
            Console.WriteLine("The system volume has been increased.");
        }

        public static void DecreaseVolume()
        {
            var defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            defaultPlaybackDevice.Volume = Math.Max(defaultPlaybackDevice.Volume - 5, 0);
            Console.WriteLine("The system volume has been Decrease.");
        }

        public static void MuteVolume()
        {
            var defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            defaultPlaybackDevice.Volume = 0;
            Console.WriteLine("The system sound was completely turned off.");
        }

      /*  public static void MuteMicrophone()
        {
            //var enumerator = new MMDeviceEnumerator();
            var mic = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            mic.AudioEndpointVolume.Mute = true;
            Console.WriteLine("mic mute.");
        }
      */
        public static void UnmuteMicrophone()
        {
            var enumerator = new MMDeviceEnumerator();
            var mic = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            mic.AudioEndpointVolume.Mute = false;
            Console.WriteLine("mic open.");
        }

        public static void MuteHeadphones()
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                if (device.FriendlyName.ToLower().Contains("headphones") || device.FriendlyName.ToLower().Contains("kulaklık"))
                {
                    device.AudioEndpointVolume.Mute = true;
                    Console.WriteLine($"The headset is silenced: {device.FriendlyName}");
                    return;
                }
            }

            Console.WriteLine("The headset device was not found.");
        }

        public static void UnmuteHeadphones()
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                if (device.FriendlyName.ToLower().Contains("headphones") || device.FriendlyName.ToLower().Contains("kulaklık"))
                {
                    device.AudioEndpointVolume.Mute = false;
                    Console.WriteLine($"Headphones sound turned on: {device.FriendlyName}");
                    return;
                }
            }

            Console.WriteLine("The headset device was not found.");
        }

    }


}