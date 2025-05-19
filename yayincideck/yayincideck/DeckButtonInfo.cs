using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static yayincideck.Form1;
using System.Text.Json;
using System.IO;
using System.Windows.Forms;
namespace yayincideck
{
    internal class DeckButtonInfo
    {
        private Form1 mainForm;

        public string Text { get; set; }
        public string FontFamily { get; set; }
        public float FontSize { get; set; }
        public string ForeColor { get; set; }
        public string BackgroundColor { get; set; }
        public string TextAlign { get; set; }
        public string AssignedAction { get; set; }
        public string PanelName { get; set; }
        public string Imgas { get; set; }

        public DeckButtonInfo(Form1 form)
        {
            mainForm = form;
        }

        public void SaveDroppedButtonsToJson(string filePath)
        {
            var buttonDataList = new List<DeckButtonInfo>();

            foreach (var button in mainForm.droppedButtons)
            {
                string action = mainForm.buttonActions.ContainsKey(button) ? mainForm.buttonActions[button].ToString() : "None";
                string panelName = button.Parent?.Name ?? "Unknown";
                string base64Image = null;

                if (button.BackgroundImage != null)
                {
                    base64Image = ConvertImageToBase64(button.BackgroundImage);
                }


                buttonDataList.Add(new DeckButtonInfo(mainForm)
                {
                    Text = button.Text,
                    FontFamily = button.Font.FontFamily.Name,
                    FontSize = button.Font.Size,
                    ForeColor = ColorTranslator.ToHtml(button.ForeColor),
                    BackgroundColor = ColorTranslator.ToHtml(button.BackColor),
                    TextAlign = button.TextAlign.ToString(),
                    AssignedAction = action,
                    PanelName = panelName,
                    Imgas = base64Image

                });
            }

            string json = JsonSerializer.Serialize(buttonDataList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private string ConvertImageToBase64(Image image)
        {
            if (image == null) return null;

            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        public Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                return Image.FromStream(ms);
            }
        }



    }


}
