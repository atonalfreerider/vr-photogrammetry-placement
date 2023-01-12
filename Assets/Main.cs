using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;
using File = UnityEngine.Windows.File;
using Rectangle = Shapes.Rectangle;

public class Main : MonoBehaviour
{
    public string PhotoFolderPath;
    string jsonPath => Path.Combine(PhotoFolderPath, "positions.json");

    readonly Dictionary<string, GameObject> pics = new();

    void Start()
    {
        DirectoryInfo root = new DirectoryInfo(PhotoFolderPath);
        int count = 0;
        foreach (FileInfo file in root.EnumerateFiles("*.jpg"))
        {
            Rectangle photo = Instantiate(NewCube.textureRectPoly, transform, false);
            photo.gameObject.SetActive(true);
            photo.gameObject.name = file.FullName;
            
            Bitmap bitmap = new Bitmap(file.FullName);

            byte[] bytes = File.ReadAllBytes(file.FullName);
            photo.LoadTexture(bytes);
                
            photo.transform.Translate(Vector3.down * count *.01f);
            photo.transform.localScale = new Vector3(bitmap.Width, 1, bitmap.Height) * .001f;
            
            photo.AddCollider();
            
            pics.Add(file.FullName, photo.gameObject);
            count++;
        }

        if (File.Exists(jsonPath))
        {
            string json = System.IO.File.ReadAllText(jsonPath);
            Dictionary<string, PositionAndRotation> fromJsonPics = JsonConvert.DeserializeObject<Dictionary<string, PositionAndRotation>>(json);

            foreach (KeyValuePair<string,PositionAndRotation> positionAndRotation in fromJsonPics)
            {
                pics[positionAndRotation.Key].gameObject.transform.localPosition = positionAndRotation.Value.positionVector3;
                pics[positionAndRotation.Key].gameObject.transform.localRotation = positionAndRotation.Value.rotationQuaternion;
            }
        }
    }
    
    [Serializable]
    class PositionAndRotation
    {
        public float positionX;
        public float positionY;
        public float positionZ;
        
        public float rotationX;
        public float rotationY;
        public float rotationZ;
        
        [JsonIgnore]
        public Vector3 positionVector3 => new(positionX, positionY, positionZ);
        [JsonIgnore]
        public Quaternion rotationQuaternion => Quaternion.Euler(new Vector3(rotationX, rotationY, rotationZ));
        
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

    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }
            
            Dictionary<string, PositionAndRotation> picsPos = pics.ToDictionary(
                x => x.Key,
                x=> new PositionAndRotation(x.Value.transform.localPosition, x.Value.transform.localRotation.eulerAngles));

            string dictionaryString = JsonConvert.SerializeObject(picsPos, Formatting.Indented);
            System.IO.File.WriteAllText(jsonPath, dictionaryString);
            
            Debug.Log("Saved to " + jsonPath);
        }
    }
}