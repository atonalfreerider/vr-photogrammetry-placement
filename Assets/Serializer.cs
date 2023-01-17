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
        public string SourceFile { get; set; }
        public DateTime GPSDateTime { get; set; }
        public float GPSLongitude { get; set; }
        public float GPSAltitude { get; set; }
        public float GPSLatitude { get; set; }
        public float GPSImgDirection { get; set; }
        public float GPSPitch { get; set; }
        public float GPSRoll { get; set; }

        public GeoTag(
            string sourceFile, 
            DateTime gpsDateTime,
            float longitude, float altitude, float latitude, 
            float imgDirection, float pitch, float roll)
        {
            SourceFile = sourceFile;
            GPSDateTime = gpsDateTime;
            GPSLongitude = longitude;
            GPSAltitude = altitude;
            GPSLatitude = latitude;
            GPSImgDirection = imgDirection;
            GPSPitch = pitch;
            GPSRoll = roll;
        }
    }
    
    [PublicAPI]
    sealed class ContractMap : ClassMap<GeoTag>
    {
        public ContractMap()
        {
            Map(m => m.SourceFile).Index(0).Name("SourceFile");
            Map(m => m.GPSDateTime).Index(1).Name("XMP:GPSDateTime");
            Map(m => m.GPSLongitude).Index(2).Name("XMP:GPSLongitude");
            Map(m => m.GPSAltitude).Index(3).Name("XMP:GPSAltitude");
            Map(m => m.GPSLatitude).Index(4).Name("XMP:GPSLatitude");
            Map(m => m.GPSImgDirection).Index(5).Name("XMP:GPSImgDirection");
            Map(m => m.GPSPitch).Index(6).Name("XMP:GPSPitch");
            Map(m => m.GPSRoll).Index(7).Name("XMP:GPSRoll");
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

    public void SerializeGeoTag(Dictionary<string, GameObject> pics, Vector3 sceneOffset, Dictionary<string, DateTime> picsByDateTime)
    {
        if (File.Exists(csvPath))
        {
            File.Delete(csvPath);
        }

        IEnumerable<GeoTag> picsPos = pics.Select(
            x => ToGeoTag(
                x.Key,
                x.Value.transform.localPosition,
                sceneOffset,
                x.Value.transform.localRotation.eulerAngles,
                picsByDateTime[x.Key]));

        CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            Encoding = Encoding.UTF8
        };
        using (StreamWriter writer = new StreamWriter(csvPath))
        using (CsvWriter csv = new CsvWriter(writer, config))
        {
            csv.Context.RegisterClassMap<ContractMap>();
            csv.WriteRecords(picsPos);
        }

        Debug.Log("Saved to " + csvPath);
    }

    static GeoTag ToGeoTag(string sourceFile, Vector3 meterVector3, Vector3 offset, Vector3 euler, DateTime dateTime)
    {
        float longitude = meterVector3.x / 111319.9f;
        float latitude = meterVector3.z / 111319.9f;

        return new GeoTag(
            sourceFile,
            dateTime,
            longitude + offset.x,
            latitude + offset.y,
            meterVector3.y + offset.z,
            euler.x,
            euler.y,
            euler.z);
    }
}