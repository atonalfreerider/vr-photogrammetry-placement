using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;
using File = UnityEngine.Windows.File;
using Rectangle = Shapes.Rectangle;

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

    class ImgMetadata
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
        DirectoryInfo root = new DirectoryInfo(PhotoFolderPath);

        int count = 0;
        foreach (FileInfo file in root.EnumerateFiles("*.jpg"))
        {
            GameObject container = new(file.FullName);
            
            Rectangle photo = Instantiate(NewCube.textureRectPoly, transform, false);
            photo.gameObject.SetActive(true);
            photo.gameObject.name = file.FullName;

            ImgMetadata imgMeta = GetExifImgSizeAndFocalLength(file.FullName);
            
            byte[] bytes = File.ReadAllBytes(file.FullName);
            photo.LoadTexture(bytes);
            photo.transform.localScale = new Vector3(imgMeta.Width, 1, imgMeta.Height) * .001f;
            photo.transform.SetParent(container.transform);
            photo.transform.Rotate(Vector3.right, -90);
            photo.transform.Translate(Vector3.down * imgMeta.FocalLength * .1f);

            GameObject focalSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            focalSphere.transform.SetParent(container.transform, true);
            focalSphere.transform.localScale = Vector3.one * .1f;
            focalSphere.GetComponent<SphereCollider>().isTrigger = true;
            focalSphere.GetComponent<SphereCollider>().radius = 3f;

            pics.Add(file.FullName, container);
            picsByDate.Add(file.FullName, file.CreationTime.ToUniversalTime());
            
            container.transform.SetParent(transform, false);
            container.transform.Translate(Vector3.back * count * .01f);
            count++;
        }

        if (File.Exists(jsonPath))
        {
            string json = System.IO.File.ReadAllText(jsonPath);
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
    }

    ImgMetadata GetExifImgSizeAndFocalLength(string path)
    {
        string[] args = {ExifToolLocation, "-s", "-ImageSize", "-FocalLength", "-focallength35efl",  path};
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
        
        float focal = float.Parse(lines[1][(lines[1].IndexOf(':') + 1)..].Replace("mm", ""));
        string widthByHeight = lines[0][(lines[0].IndexOf(':') + 1)..];
        if(!string.IsNullOrEmpty(lines[2]))
        {
            string focal35 = lines[2][(lines[2].IndexOf("equivalent:", StringComparison.Ordinal) + 12)..];
            focal35 = focal35[..focal35.IndexOf(' ')];
            focal = float.Parse(focal35);
        }


        return new ImgMetadata(
            focal, 
            int.Parse(widthByHeight.Split('x')[0]),
            int.Parse(widthByHeight.Split('x')[1]));
    }

    void WriteGeoData()
    {
        string[] args = {ExifToolLocation, $"-csv=\"{Path.Combine(PhotoFolderPath, "geo.csv")}\"", PhotoFolderPath};
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