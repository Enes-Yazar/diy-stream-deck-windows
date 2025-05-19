using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;
using TMPro;
using System.Collections.Generic;

public class QRCodeScanner : MonoBehaviour
{
    public GameObject scannerUI;
    private ButtonLoader buttonLoader; // ButtonLoader.cs referans�
   public TextMeshProUGUI texts;
    public void Start()
    {
        if (NativeCamera.IsCameraBusy())
            return;

        NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                StartCoroutine(DecodeQRCode(path));
            }
        }, maxSize: 512);

    }
    void Awake()
    {
        buttonLoader = FindObjectOfType<ButtonLoader>();
    }

    IEnumerator DecodeQRCode(string path)
    {
        byte[] imageData = System.IO.File.ReadAllBytes(path);

        // Form nesnesi oluştur
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("file", imageData, "qrimage.png", "image/png"));

        UnityWebRequest request = UnityWebRequest.Post("https://api.qrserver.com/v1/read-qr-code/", formData);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler.text;
            Debug.Log("QR çözüm JSON: " + result);

            string ipUrl = ExtractURLFromJson(result);
            Debug.Log("Çözülmüş URL: " + ipUrl);

            if (!string.IsNullOrEmpty(ipUrl))
            {
                buttonLoader.url = ipUrl;
              //  yield return StartCoroutine(buttonLoader.LoadButtons());

                if (scannerUI != null)
                    scannerUI.SetActive(false);
            }
            else
            {
                texts.text = "QR kodu çözümleme hatası: IP bulunamadı.";
            }
        }
        else
        {
            Debug.LogError("QR hatası: " + request.error + " (" + request.responseCode + ")");
            texts.text = "QR kod sunucu hatası: " + request.responseCode;
        }
    }


    string ExtractURLFromJson(string json)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<Wrapper>(FixJson(json));
            return wrapper.symbol[0].data;
        }
        catch
        {
            return null;
        }
    }

    [System.Serializable]
    public class Wrapper
    {
        public Symbol[] symbol;
    }

    [System.Serializable]
    public class Symbol
    {
        public string data;
        public string error;
    }

    string FixJson(string value)
    {
        // JsonUtility i�in d�zeltme
        value = value.Trim('[', ']');
        return value;
    }
}
