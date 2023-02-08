using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;
#if !UNITY_ANDROID
using System.Drawing;
#endif

public class Main : MonoBehaviour
{
    public string PhotoFolderPath;
    public float SceneLongitude;
    public float ScneneAltitude;
    public float SceneLatitude;
    public string ExifToolLocation;

    Vector3 sceneOffset => new(SceneLongitude, ScneneAltitude, SceneLatitude);

    string jsonPath => Path.Combine(PhotoFolderPath, "positions.json");

    readonly Dictionary<string, GameObject> pics = new();
    readonly Dictionary<string, DateTime> picsByDate = new();

    readonly Dictionary<string, ImgMetadata> picsToCamera = new();

    public class ImgMetadata
    {
        public readonly float FocalLength;
        public readonly int Width;
        public readonly int Height;

        public ImgMetadata(float focalLength, int width, int height)
        {
            FocalLength = focalLength;
            Width = width;
            Height = height;
        }
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(PhotoFolderPath))
        {
            DirectoryInfo root = new DirectoryInfo(PhotoFolderPath);

            int count = 0;
            foreach (FileInfo file in root.EnumerateFiles("*.png").Concat(root.EnumerateFiles("*.jpg")))
            {
                GameObject container = PhotoFromImage(
                    file.FullName,
                    file.CreationTime.ToUniversalTime(),
                    File.ReadAllBytes(file.FullName));
                container.transform.SetParent(transform, false);
                container.transform.Translate(Vector3.back * count * .01f);
                count++;
            }
        }
        else
        {
            int count = 0;
            foreach (Texture2D image in Resources.LoadAll<Texture2D>("Textures"))
            {
                GameObject container = PhotoFromImage(image.name, DateTime.Now, null, image);
                container.transform.SetParent(transform, false);
                container.transform.Translate(Vector3.back * count * .01f);
                count++;
            }
        }

        string json;
        if (File.Exists(jsonPath))
        {
            json = File.ReadAllText(jsonPath);
        }
        else
        {
            TextAsset jsonPositions = Resources.Load<TextAsset>("positions");
            json = jsonPositions != null ? jsonPositions.text : "{}";
        }

        Dictionary<string, PositionAndRotation> fromJsonPics =
            JsonConvert.DeserializeObject<Dictionary<string, PositionAndRotation>>(json);

        foreach (KeyValuePair<string, PositionAndRotation> positionAndRotation in fromJsonPics)
        {
            GameObject container = pics[positionAndRotation.Key];
            container.transform.localPosition =
                positionAndRotation.Value.positionVector3;
            container.transform.localRotation =
                positionAndRotation.Value.rotationQuaternion;
        }
    }

    GameObject PhotoFromImage(string imageName, DateTime creationTime, byte[] bytes = null, Texture2D texture2D = null)
    {
        GameObject container = new(imageName);

        Rectangle photo = Instantiate(NewCube.textureRectPoly, transform, false);
        photo.gameObject.SetActive(true);
        photo.gameObject.name = imageName;

        ImgMetadata imgMeta = null;
        if (!string.IsNullOrEmpty(ExifToolLocation))
        {
            imgMeta = GetExifImgSizeAndFocalLength(imageName);
        }
#if !UNITY_ANDROID
        else if (File.Exists(imageName))
        {
            Bitmap bitmap = new Bitmap(imageName);
            imgMeta = new ImgMetadata(28, bitmap.Width, bitmap.Height);
        }
#endif
        else if (texture2D != null)
        {
            imgMeta = new ImgMetadata(28, texture2D.width * 2, texture2D.height * 2);
        }

        if (bytes != null)
        {
            photo.LoadTexture(bytes);
        }
        else if (texture2D != null)
        {
            photo.rend.material.SetTexture(Shader.PropertyToID("_MainTex"), texture2D);
            photo.rend.material.color = new Color(1, 1, 1, .5f);
        }

        photo.transform.localScale = new Vector3(imgMeta.Width, 1, imgMeta.Height) * .001f;
        photo.transform.SetParent(container.transform);
        photo.transform.Rotate(Vector3.right, -90);
        photo.transform.Translate(Vector3.down * imgMeta.FocalLength * .1f);

        GameObject focalSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        focalSphere.transform.SetParent(container.transform, true);
        focalSphere.transform.localScale = Vector3.one * .1f;
        focalSphere.GetComponent<SphereCollider>().isTrigger = true;
        focalSphere.GetComponent<SphereCollider>().radius = 3f;

        pics.Add(imageName, container);
        picsByDate.Add(imageName, creationTime);

        picsToCamera.Add(imageName, imgMeta);
        return container;
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Serializer serializer = new Serializer(jsonPath);
            serializer.SerializeCartesian(pics);
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            // get exiftool here: https://exiftool.org/
            // run this command
            // perl C:\Image-ExifTool-12.54\exiftool.pl -csv="C:\Users\john\Desktop\TOWER_HOUSE\exterior\geo.csv" C:\Users\john\Desktop\TOWER_HOUSE\exterior
            Serializer serializer = new Serializer(Path.Combine(PhotoFolderPath, "geo.csv"));
            serializer.SerializeGeoTag(pics, sceneOffset, picsByDate);
            //WriteGeoData();
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            NerfSerializer nerfSerializer =
                new NerfSerializer(Path.Combine(new DirectoryInfo(PhotoFolderPath).Parent.FullName, "transforms.json"));
            nerfSerializer.Serialize(pics, picsToCamera, new DirectoryInfo(PhotoFolderPath).Name);
        }
    }

    ImgMetadata GetExifImgSizeAndFocalLength(string path)
    {
        string[] args =
            { ExifToolLocation, "-s", "-ImageSize", "-FocalLength", "-focallength35efl", "-Model", path };
        ProcessStartInfo processStartInfo = new()
        {
            FileName = "perl",
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        Process process = new();
        process.StartInfo = processStartInfo;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        string[] lines = output.Split("\r\n");

        string focalString = lines[1].Replace("mm", "");

        float focal = 28;
        if (!string.IsNullOrEmpty(focalString))
        {
            focal = float.Parse(ValueFromExif(focalString));
        }

        string widthByHeight = ValueFromExif(lines[0]);
        if (lines.Length > 2 && !string.IsNullOrEmpty(lines[2]))
        {
            string focal35 = lines[2][(lines[2].IndexOf("equivalent:", StringComparison.Ordinal) + 12)..];
            focal35 = focal35[..focal35.IndexOf(' ')];
            focal = float.Parse(focal35);
        }

        // string cameraModel = ValueFromExif(lines[3]);

        return new ImgMetadata(
            focal,
            int.Parse(widthByHeight.Split('x')[0]),
            int.Parse(widthByHeight.Split('x')[1]));
    }

    static string ValueFromExif(string exifString)
    {
        return exifString[(exifString.IndexOf(':') + 1)..];
    }

    void WriteGeoData()
    {
        string[] args = { ExifToolLocation, $"-csv=\"{Path.Combine(PhotoFolderPath, "geo.csv")}\"", PhotoFolderPath };
        ProcessStartInfo processStartInfo = new()
        {
            FileName = "perl",
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        Process process = new();
        process.StartInfo = processStartInfo;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        Debug.Log(output);
    }
}