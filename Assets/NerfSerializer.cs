using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public class NerfSerializer
{
    readonly string jsonPath;

    [PublicAPI]
    [Serializable]
    public class Container
    {
        public int aabb_scale = 16; // powers of 2 between 1 and 128, defines the bounding box size 
        public List<NerfFrame> frames = new();
    }

    /// <summary>
    /// from: https://github.com/NVlabs/instant-ngp/pull/1147#issuecomment-1374127391
    /// </summary>
    [PublicAPI]
    [Serializable]
    public class NerfFrame
    {
        public string file_path;
        public float sharpness = 1000f; // 0 to 1000?
        public float[][] transform_matrix;

        public float camera_angle_x = 0f;
        public float camera_angle_y = 0f;

        // focal lengths for rectangular pixels
        public float fl_x;

        public float fl_y;

        // these values are used by OPENCV
        public float k1 = 0f;
        public float k2 = 0f;
        public float k3 = 0f;

        public float k4 = 0f;

        // these values are used by OPENCV for distortion
        public float p1 = 0f;
        public float p2 = 0f;
        public bool is_fisheye = false;

        // center of image
        public float cx;
        public float cy;

        // dimensions
        public float w;
        public float h;

        public NerfFrame(
            string file_path,
            float[][] transform_matrix,
            float w, float h,
            float fl_x)
        {
            this.file_path = file_path;
            this.transform_matrix = transform_matrix;

            cx = w / 2f;
            cy = h / 2f;
            this.w = w;
            this.h = h;
            this.fl_x = fl_x;
            fl_y = fl_x;
        }
    }

    public NerfSerializer(string path)
    {
        jsonPath = path;
    }

    public void Serialize(
        Dictionary<string, GameObject> pics,
        Dictionary<string, Main.ImgMetadata> picToCamera,
        string rootName)
    {
        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }

        Container container = new();

        foreach (KeyValuePair<string, GameObject> keyValuePair in pics)
        {
            keyValuePair.Value.transform.Rotate(Vector3.left * 90f);
            Matrix4x4 transformMatrix4 = keyValuePair.Value.transform.localToWorldMatrix;
            Vector3 pos = transformMatrix4.GetColumn(3);
            pos = new Vector3(pos.x, pos.z, pos.y);
            transformMatrix4 = transformMatrix4.inverse;
            transformMatrix4.SetColumn(3, pos);

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
                transformMatrixArray,
                picToCamera[keyValuePair.Key].Width,
                picToCamera[keyValuePair.Key].Height,
                picToCamera[keyValuePair.Key].FocalLength * 100f);

            container.frames.Add(nerfFrame);
        }

        string cameraJsonString = JsonConvert.SerializeObject(container, Formatting.Indented);
        File.WriteAllText(jsonPath, cameraJsonString);

        Debug.Log("Saved to " + jsonPath);
    }

    static Matrix4x4 LeftHandMatrixFromRightHandMatrix(Matrix4x4 rightHandMatrix)
    {
        Matrix4x4 leftHandMatrix = rightHandMatrix;
        leftHandMatrix.m02 = -rightHandMatrix.m02;
        leftHandMatrix.m12 = -rightHandMatrix.m12;
        leftHandMatrix.m22 = -rightHandMatrix.m22;
        leftHandMatrix.m32 = -rightHandMatrix.m32;
        return leftHandMatrix;
    }
}