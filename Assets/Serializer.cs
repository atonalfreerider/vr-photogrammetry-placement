using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using File = UnityEngine.Windows.File;

[Serializable]
public class PositionAndRotation
{
    public float positionX;
    public float positionY;
    public float positionZ;

    public float rotationX;
    public float rotationY;
    public float rotationZ;

    [JsonIgnore] public Vector3 positionVector3 => new(positionX, positionY, positionZ);
    [JsonIgnore] public Quaternion rotationQuaternion => Quaternion.Euler(new Vector3(rotationX, rotationY, rotationZ));

    public PositionAndRotation(Vector3 position, Vector3 rotation)
    {
        positionX = position.x;
        positionY = position.y;
        positionZ = position.z;

        rotationX = rotation.x;
        rotationY = rotation.y;
        rotationZ = rotation.z;
    }
}

public class Serializer
{
    [PublicAPI]
    class GeoTag
    {
        public float GPSLongitude { get; set; }
        public float GPSAltitude { get; set; }
        public float GPSLatitude { get; set; }
        public float GPSImgDirection { get; set; }
        public float GPSPitch { get; set; }
        public float GPSRoll { get; set; }

        public GeoTag(float longitude, float altitude, float latitude, float imgDirection, float pitch, float roll)
        {
            GPSLongitude = longitude;
            GPSAltitude = altitude;
            GPSLatitude = latitude;
            GPSImgDirection = imgDirection;
            GPSPitch = pitch;
            GPSRoll = roll;
        }
    }

    readonly string jsonPath;
    readonly string csvPath;

    public Serializer(string path)
    {
        jsonPath = path;
        csvPath = path;
    }

    public void SerializeCartesian(Dictionary<string, GameObject> pics)
    {
        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }

        Dictionary<string, PositionAndRotation> picsPos = pics.ToDictionary(
            x => x.Key,
            x => new PositionAndRotation(
                x.Value.transform.localPosition,
                x.Value.transform.localRotation.eulerAngles));

        string dictionaryString = JsonConvert.SerializeObject(picsPos, Formatting.Indented);
        System.IO.File.WriteAllText(jsonPath, dictionaryString);

        Debug.Log("Saved to " + jsonPath);
    }

    public void SerializeGeoTag(Dictionary<string, GameObject> pics, Vector3 sceneOffset)
    {
        if (File.Exists(csvPath))
        {
            File.Delete(csvPath);
        }

        Dictionary<string, GeoTag> picsPos = pics.ToDictionary(
            x => x.Key,
            x => ToGeoTag(
                x.Value.transform.localPosition,
                sceneOffset,
                x.Value.transform.localRotation.eulerAngles));

        CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            Encoding = Encoding.UTF8
        };
        using (StreamWriter writer = new StreamWriter(csvPath))
        using (CsvWriter csv = new CsvWriter(writer, config))
        {
            csv.WriteRecords(picsPos);
        }

        Debug.Log("Saved to " + csvPath);
    }

    static GeoTag ToGeoTag(Vector3 meterVector3, Vector3 offset, Vector3 euler)
    {
        float longitude = meterVector3.x / 111319.9f;
        float latitude = meterVector3.z / 111319.9f;

        return new GeoTag(
            longitude + offset.x,
            latitude + offset.y,
            meterVector3.y + offset.z,
            euler.x,
            euler.y,
            euler.z);
    }
}