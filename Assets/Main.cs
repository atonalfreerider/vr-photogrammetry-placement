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

            Tuple<string, string> ImgSizeAndFocal = GetExifImgSizeAndFocalLength(file.FullName);
            float focal = float.Parse(ImgSizeAndFocal.Item2.Replace("mm", ""));
            int height = int.Parse(ImgSizeAndFocal.Item1.Split('x')[1]);
            int width = int.Parse(ImgSizeAndFocal.Item1.Split('x')[0]);
            
            byte[] bytes = File.ReadAllBytes(file.FullName);
            photo.LoadTexture(bytes);
            photo.transform.localScale = new Vector3(width, 1, height) * .001f;
            photo.transform.SetParent(container.transform);
            photo.transform.Rotate(Vector3.right, -90);
            photo.transform.Translate(Vector3.down * focal);

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
            // perl C:\Image-ExifTool-12.54\exiftool.pl -csv="C:\Users\john\Desktop\TOWER_HOUSE\exterior-original\geo.csv" C:\Users\john\Desktop\TOWER_HOUSE\exterior-original
            Serializer serializer = new Serializer(Path.Combine(PhotoFolderPath, "geo.csv"));
            serializer.SerializeGeoTag(pics, sceneOffset, picsByDate);
            WriteGeoData();
        }
    }

    Tuple<string, string> GetExifImgSizeAndFocalLength(string path)
    {
        string[] args = {ExifToolLocation, "-s", "-ImageSize", "-FocalLength",  path};
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
        return new Tuple<string, string>(lines[0][(lines[0].IndexOf(':') + 1)..], lines[1][(lines[1].IndexOf(':') + 1)..]);
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