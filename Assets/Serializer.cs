using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CsvHelper.Configuration;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class PositionAndRotation
{
    public float positionX;
    public float positionY;
    public float positionZ;

    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public float rotationW;
    
    public float focal;

    [JsonIgnore] public Vector3 positionVector3 => new(positionX, positionY, positionZ);
    [JsonIgnore] public Quaternion rotationQuaternion =>new(rotationX, rotationY, rotationZ, rotationW);

    public PositionAndRotation(Vector3 position, Quaternion rotation, float focal)
    {
        positionX = position.x;
        positionY = position.y;
        positionZ = position.z;

        rotationX = rotation.x;
        rotationY = rotation.y;
        rotationZ = rotation.z;
        rotationW = rotation.w;
        
        this.focal = focal;
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

    public void Serialize(Dictionary<string, CameraSetup> pics)
    {
        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }

        Dictionary<string, PositionAndRotation> picsPos = pics.ToDictionary(
            x => x.Key,
            x => new PositionAndRotation(
                x.Value.transform.localPosition,
                x.Value.transform.localRotation,
                x.Value.Focal));

        string dictionaryString = JsonConvert.SerializeObject(picsPos, Formatting.Indented);
        File.WriteAllText(jsonPath, dictionaryString);

        Debug.Log("Saved to " + jsonPath);
    }
}