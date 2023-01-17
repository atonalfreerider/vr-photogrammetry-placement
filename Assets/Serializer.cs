using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Windows;

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
    readonly string jsonPath;

    public Serializer(string path)
    {
        jsonPath = path;
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
        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }

        Dictionary<string, PositionAndRotation> picsPos = pics.ToDictionary(
            x => x.Key,
            x => new PositionAndRotation(
                MeterVector3ToLongitudeAltitudeLatitude(x.Value.transform.localPosition) + sceneOffset,
                x.Value.transform.localRotation.eulerAngles));

        string dictionaryString = JsonConvert.SerializeObject(picsPos, Formatting.Indented);
        System.IO.File.WriteAllText(jsonPath, dictionaryString);

        Debug.Log("Saved to " + jsonPath);
    }
    
    static Vector3 MeterVector3ToLongitudeAltitudeLatitude(Vector3 meterVector2)
    {
        float longitude = meterVector2.x / 111319.9f;
        float latitude = meterVector2.z / 111319.9f;

        return new Vector3(longitude, meterVector2.y, latitude);
    }
}