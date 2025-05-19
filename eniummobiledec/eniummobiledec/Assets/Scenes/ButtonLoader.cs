using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class ButtonData
{
    public string Text;
    public string AssignedAction;
    public string Imgas;
    public string backgroundColor;
    public string ForeColor;

    public string TextAlign;
    public string FontFamily;
    public float FontSize;
    public string PanelName;

}

public class ButtonLoader : MonoBehaviour
{
    public string url = "http://192.168.0.13:5000/buttons.json";
    public GameObject buttonPrefab;
    public Transform buttonParent;
    public TMP_Text buttonTextPrefab;
    public float refreshInterval = 0.1f; // Kaç saniyede bir güncelleme yapılacak
    private List<GameObject> loadedButtons = new List<GameObject>();

    void Start()
    {
        StartCoroutine(UpdateLoop());

    }
    IEnumerator UpdateLoop()
    {
        while (true)
        {
            yield return LoadButtons();
            yield return new WaitForSeconds(refreshInterval);
        }
    }
    TextAlignmentOptions GetTMPAlignment(string align)
    {
        switch (align?.ToLower())
        {
            case "topleft": return TextAlignmentOptions.TopLeft;
            case "topcenter": return TextAlignmentOptions.Top;
            case "topright": return TextAlignmentOptions.TopRight;

            case "middleleft":
            case "midleft":
            case "centerleft":
                return TextAlignmentOptions.Left;

            case "middlecenter":
            case "midcenter":
            case "center":
            case "centercenter":
                return TextAlignmentOptions.Center;

            case "middleright":
            case "midright":
            case "centerright":
                return TextAlignmentOptions.Right;

            case "bottomleft": return TextAlignmentOptions.BottomLeft;
            case "bottomcenter": return TextAlignmentOptions.Bottom;
            case "bottomright": return TextAlignmentOptions.BottomRight;

            case "justified": return TextAlignmentOptions.Justified;

            default: return TextAlignmentOptions.Center;
        }
    }

  public  IEnumerator LoadButtons()
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Hata: " + www.error);
            yield break;
        }
        foreach (GameObject btn in loadedButtons)
        {
            Destroy(btn);
        }
        loadedButtons.Clear();
        string json = www.downloadHandler.text;
        ButtonData[] buttons = JsonHelper.FromJson<ButtonData>(json);


        foreach (var data in buttons)
        {
            Transform targetParent = GetPanelByName(data.PanelName);
            GameObject newButton = Instantiate(buttonPrefab, targetParent);
            loadedButtons.Add(newButton); // Listeye ekle
        
            // Buton tıklama işlevi
            Button btn = newButton.GetComponent<Button>();
            if (btn != null)
            {
                //Debug.Log("Buton bulundu, listener ekleniyor: " + data.AssignedAction);
                btn.onClick.AddListener(() => OnButtonClick(data.AssignedAction));
            }
            else
            {
                Debug.LogError("Buton bileşeni bulunamadı!");
            }

            // Arka plan rengi
            Image bg = newButton.GetComponent<Image>();
            if (bg != null)
                bg.color = HexToColor(data.backgroundColor);

            // Metin içeriği ve rengi
            TMP_Text Text = newButton.GetComponentInChildren<TMP_Text>();
            if (Text != null)
            {
                Text.text = data.Text;
                Text.color = HexToColor(data.ForeColor);
                Text.alignment = GetTMPAlignment(data.TextAlign);
                Text.enableAutoSizing = false;

                if (data.FontSize > 0)
                    Text.fontSize = data.FontSize;
                Text.margin = new Vector4(0, 0, 0, 0);
                Text.raycastTarget = false;

                RectTransform rt = Text.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.offsetMin = new Vector2(10, rt.offsetMin.y);
                    rt.offsetMax = new Vector2(-10, rt.offsetMax.y);
                }
            }


            // Base64 görsel yükleme
            if (!string.IsNullOrEmpty(data.Imgas))
            {
                Image img = newButton.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = Base64ToSprite(data.Imgas);
                    img.type = Image.Type.Sliced; // Gerekirse Sliced yap (boyutlandırılabilir)
                }
            }
        }
    }
    Transform GetPanelByName(string panelName)
    {
        GameObject panelObj = GameObject.Find(panelName);
        if (panelObj != null)
            return panelObj.transform;

        Debug.LogWarning($"Panel bulunamadı: {panelName}, varsayılan parent kullanılıyor.");
        return buttonParent; // Bulamazsa fallback
    }

    Sprite Base64ToSprite(string Imgas)
    {
        try
        {
            byte[] imageBytes = Convert.FromBase64String(Imgas);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

        }
        catch (Exception e)
        {
            Debug.LogError("Base64 görsel dönüştürme hatası: " + e.Message);
            return null;
        }
    }

    void OnButtonClick(string action)
    {
        StartCoroutine(SendCommandToServer(action));

        Debug.Log("Butona tıklandı, action: " + action);
        if (string.IsNullOrEmpty(action))
        {
            Debug.LogWarning("Action boş veya null, komut gönderilmiyor!");
            return;
        }
    }


    IEnumerator SendCommandToServer(string action)
    {
        string url = $"http://192.168.0.13:5001/?command={action}";
        Debug.Log("Komut gönderiliyor: " + url);

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ERROR] Komut gönderilemedi: {www.error}");
            }
            else
            {
                Debug.Log($"[BAŞARILI] Komut gönderildi: {action}");

            }
        }
    }


    Color HexToColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }
        return Color.white;
    }

}
