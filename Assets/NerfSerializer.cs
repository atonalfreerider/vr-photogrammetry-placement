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

    /// <summary>
    /// Write the Unity Left Hand Side format to Right Hand Side format to be read by NeRF
    /// </summary>
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
            Transform picTrans = keyValuePair.Value.transform;
            
            // rotate and then invert the camera
            // BUG: this is not working even though the test matrix comes back correct
            picTrans.Rotate(Vector3.left * 90f);
            picTrans.rotation = Quaternion.Inverse(picTrans.rotation);
            
            // get the matrix
            Matrix4x4 transformMatrix4 = picTrans.localToWorldMatrix;
            
            // flip z and y in the position
            Vector4 pos = transformMatrix4.GetColumn(3);
            pos = new Vector4(pos.x, pos.z, pos.y, pos.w);
            
            // overwrite the position in the matrix
            transformMatrix4.SetColumn(3, pos);

            // save the matrix to a jagged array
            float[][] transformMatrixArray = new float[4][];
            for (int i = 0; i < 4; i++)
            {
                transformMatrixArray[i] = new float[4];
                for (int j = 0; j < 4; j++)
                {
                    transformMatrixArray[i][j] = transformMatrix4[i, j];
                }
            }

            Main.ImgMetadata picMeta = picToCamera[keyValuePair.Key];
            NerfFrame nerfFrame = new NerfFrame(
                $"./{rootName}/{Path.GetFileName(keyValuePair.Key)}",
                transformMatrixArray,
                picMeta.Width,
                picMeta.Height,
                picMeta.FocalLength * 100f);

            container.frames.Add(nerfFrame);
        }

        string cameraJsonString = JsonConvert.SerializeObject(container, Formatting.Indented);
        File.WriteAllText(jsonPath, cameraJsonString);

        Debug.Log("Saved to " + jsonPath);
        
        // test the matrix
        DoDeserialize(jsonPath);
    }

    public static void DoDeserialize(string path)
    {
        string jsonString = File.ReadAllText(path);
        Container container = JsonConvert.DeserializeObject<Container>(jsonString);
        foreach (NerfFrame frame in container.frames)
        {
            Matrix4x4 matrix4X4 = Matrix4X4fromFloatArray(frame.transform_matrix);

            GameObject flat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flat.name = frame.file_path;
            SetTransformPositionRotationFrommMatrix4X4(flat.transform, matrix4X4);
        }
    }

    static void SetTransformPositionRotationFrommMatrix4X4(Transform transform, Matrix4x4 matrix4X4)
    {
        Vector3 pos = matrix4X4.GetColumn(3);
        pos = new Vector3(pos.x, pos.z, pos.y);
        transform.position = pos;

        transform.rotation = matrix4X4.inverse.rotation;
        transform.Rotate(Vector3.right, 90);
    }

    static Matrix4x4 Matrix4X4fromFloatArray(float[][] array)
    {
        Matrix4x4 matrix4X4 = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                matrix4X4[i, j] = array[i][j];
            }
        }

        return matrix4X4;
    }
}