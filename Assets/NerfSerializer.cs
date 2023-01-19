using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public class NerfSerializer
{
    readonly string jsonPath;

    [PublicAPI]
    [Serializable]
    public class NerfCamera
    {
        public float camera_angle_x;
        public float camera_angle_y;
        public float fl_x;
        public float fl_y;
        public float k1;
        public float k2;
        public float k3;
        public float k4;
        public float p1;
        public float p2;
        public bool is_fisheye = false;
        public float cx;
        public float cy;
        public float w;
        public float h;
        public float aabb_scale;
        public List<NerfFrame> frames = new();

        public NerfCamera(
            float cx = 0, float cy = 0, float w = 0, float h = 0,
            int aabb_scale = 16, // BUG: made up number 
            float camera_angle_x = 0, float camera_angle_y = 0,
            float fl_x = 0, float fl_y = 0,
            float k1 = 0.5f, // BUG: made up number
            float k2 = 0, float k3 = 0, float k4 = 0,
            float p1 = 0.01f, // BUG: made up number
            float p2 = 0.01f, // BUG: made up number
            bool is_fisheye = false)
        {
            this.cx = cx;
            this.cy = cy;
            this.w = w;
            this.h = h;
            this.aabb_scale = aabb_scale;
            this.camera_angle_x = camera_angle_x;
            this.camera_angle_y = camera_angle_y;
            this.fl_x = fl_x;
            this.fl_y = fl_y;
            this.k1 = k1;
            this.k2 = k2;
            this.k3 = k3;
            this.k4 = k4;
            this.p1 = p1;
            this.p2 = p2;
            this.is_fisheye = is_fisheye;
        }
    }

    [PublicAPI]
    [Serializable]
    public class NerfFrame
    {
        public string file_path;
        public float sharpness;
        public float[][] transform_matrix;

        public NerfFrame(string file_path, float sharpness, float[][] transform_matrix)
        {
            this.file_path = file_path;
            this.sharpness = sharpness;
            this.transform_matrix = transform_matrix;
        }
    }

    public NerfSerializer(string path)
    {
        jsonPath = path;
    }

    public void Serialize(
        Dictionary<string, NerfCamera> cameras,
        Dictionary<string, GameObject> pics,
        Dictionary<string, string> picToCamera,
        string rootName)
    {
        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }

        foreach (KeyValuePair<string, GameObject> keyValuePair in pics)
        {
            keyValuePair.Value.transform.Rotate(Vector3.up, 180); // BUG: testing
            Matrix4x4 transformMatrix4 = keyValuePair.Value.transform.localToWorldMatrix;
            float[][] transformMatrixArray = new float[4][];
            for (int i = 0; i < 4; i++)
            {
                transformMatrixArray[i] = new float[4];
                for (int j = 0; j < 4; j++)
                {
                    transformMatrixArray[i][j] = transformMatrix4[i, j];
                }
            }

            NerfFrame nerfFrame = new NerfFrame(
                $"./{rootName}/{Path.GetFileName(keyValuePair.Key)}",
                1000f, // BUG: made up number
                transform_matrix: transformMatrixArray);

            cameras[picToCamera[keyValuePair.Key]].frames.Add(nerfFrame);
        }

        // BUG: only takes first camera
        string cameraJsonString = JsonConvert.SerializeObject(cameras.Values.ToArray()[0], Formatting.Indented);
        File.WriteAllText(jsonPath, cameraJsonString);

        Debug.Log("Saved to " + jsonPath);
    }
}