using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;
using File = UnityEngine.Windows.File;
using Rectangle = Shapes.Rectangle;

public class Main : MonoBehaviour
{
    public string PhotoFolderPath;
    public float SceneLongitude;
    public float ScneneAltitude;
    public float SceneLatitude;

    Vector3 sceneOffset => new(SceneLongitude, ScneneAltitude, SceneLatitude);
    
    Serializer serializer;
    string jsonPath => Path.Combine(PhotoFolderPath, "positions.json");

    readonly Dictionary<string, GameObject> pics = new();

    void Start()
    {
        DirectoryInfo root = new DirectoryInfo(PhotoFolderPath);
        serializer = new Serializer(jsonPath);
        int count = 0;
        foreach (FileInfo file in root.EnumerateFiles("*.jpg"))
        {
            Rectangle photo = Instantiate(NewCube.textureRectPoly, transform, false);
            photo.gameObject.SetActive(true);
            photo.gameObject.name = file.FullName;

            Bitmap bitmap = new Bitmap(file.FullName);

            byte[] bytes = File.ReadAllBytes(file.FullName);
            photo.LoadTexture(bytes);

            photo.transform.Translate(Vector3.down * count * .01f);
            photo.transform.localScale = new Vector3(bitmap.Width, 1, bitmap.Height) * .001f;

            photo.AddCollider();

            pics.Add(file.FullName, photo.gameObject);
            count++;
        }

        if (File.Exists(jsonPath))
        {
            string json = System.IO.File.ReadAllText(jsonPath);
            Dictionary<string, PositionAndRotation> fromJsonPics =
                JsonConvert.DeserializeObject<Dictionary<string, PositionAndRotation>>(json);

            foreach (KeyValuePair<string, PositionAndRotation> positionAndRotation in fromJsonPics)
            {
                pics[positionAndRotation.Key].gameObject.transform.localPosition =
                    positionAndRotation.Value.positionVector3;
                pics[positionAndRotation.Key].gameObject.transform.localRotation =
                    positionAndRotation.Value.rotationQuaternion;
            }
        }
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            serializer.SerializeCartesian(pics);
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            foreach (KeyValuePair<string, GameObject> keyValuePair in pics)
            {
                Vector3 longitudeLatitudeAltitude = Serializer.MeterVector3ToLongitudeAltitudeLatitude(keyValuePair.Value.transform.localPosition);
                longitudeLatitudeAltitude += sceneOffset;
                
                Debug.Log(longitudeLatitudeAltitude);
            }
        }
    }
}