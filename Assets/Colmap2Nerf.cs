using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class Colmap2Nerf
{
    public string ColmapFolder;

    public Colmap2Nerf(string colmapFolder)
    {
        ColmapFolder = colmapFolder;
    }

    class CamData
    {
        public string camName;
        public int width;
        public int height;
        public float fx;
        public int cx;
        public int cy;

        public CamData(string camName, int width, int height, float fx, int cx, int cy)
        {
            this.camName = camName;
            this.width = width;
            this.height = height;
            this.fx = fx;
            this.cx = cx;
            this.cy = cy;
        }
    }

    public void Convert()
    {
        // read cameras.txt
        string camerasTxt = File.ReadAllText(ColmapFolder + "/cameras.txt");
        Dictionary<string, CamData> cameras = new Dictionary<string, CamData>();

        string[] cameraLines = camerasTxt.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string cameraLine in cameraLines)
        {
            if (cameraLine.StartsWith('#')) continue;

            string[] cameraLineSplit = cameraLine.Split(' ');
            string cameraName = cameraLineSplit[0];
            int width = int.Parse(cameraLineSplit[2]);
            int height = int.Parse(cameraLineSplit[3]);
            float fx = float.Parse(cameraLineSplit[4]);
            int cx = int.Parse(cameraLineSplit[5]);
            int cy = int.Parse(cameraLineSplit[6]);

            cameras.Add(cameraName, new CamData(cameraName, width, height, fx, cx, cy));
        }


        // read images.txt
        string imagesTxt = File.ReadAllText(ColmapFolder + "/images.txt");

        string[] lines = imagesTxt.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        List<NerfSerializer.NerfFrame> frames = new();
        foreach (string line in lines)
        {
            if (line.EndsWith(".png"))
            {
                string[] parts = line.Split(' ');

                string cameraName = parts[0];
                CamData camData = cameras[cameraName];

                string imageName = Path.Combine(ColmapFolder, parts[9]);

                Quaternion rot = new Quaternion(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]),
                    float.Parse(parts[4]));

                Vector3 pos = new Vector3(float.Parse(parts[5]), float.Parse(parts[6]), float.Parse(parts[7]));

                Matrix4x4 viewMatrix = Matrix4x4.TRS(pos, rot, Vector3.one);


                NerfSerializer.NerfFrame nerfFrame = new NerfSerializer.NerfFrame(
                    imageName,
                    NerfSerializer.Matrix4X4toFloatArray(viewMatrix),
                    camData.width,
                    camData.height,
                    camData.fx);
                
                frames.Add(nerfFrame);
            }
        }

        NerfSerializer.Container container = new NerfSerializer.Container
        {
            frames = frames
        };

        string outPath = Path.Combine(ColmapFolder, "transforms.json");
        if (File.Exists(outPath))
        {
            File.Delete(outPath);
        }
        
        string cameraJsonString = JsonConvert.SerializeObject(container, Formatting.Indented);
        File.WriteAllText(outPath, cameraJsonString);
        
    }
}