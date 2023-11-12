using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using UnityEngine;

/// <summary>
/// Sqlite db needs to have a frames table
/// </summary>
public class SqliteInput : MonoBehaviour
{
    [Header("Input")] public string DbPath;

    enum Joints
    {
        Nose = 0,
        L_Eye = 1,
        R_Eye = 2,
        L_Ear = 3,
        R_Ear = 4,
        L_Shoulder = 5,
        R_Shoulder = 6,
        L_Elbow = 7,
        R_Elbow = 8,
        L_Wrist = 9,
        R_Wrist = 10,
        L_Hip = 11,
        R_Hip = 12,
        L_Knee = 13,
        R_Knee = 14,
        L_Ankle = 15,
        R_Ankle = 16
    }

    public List<List<List<List<Vector2>>>> ReadFrameFromDb()
    {
        List<List<List<List<Vector2>>>> allCameras = new();

        string connectionString = "URI=file:" + DbPath;


        int lastCameraId = -1;
        int lastFrameId = -1;
        int lastDancerId = -1;
        
        List<List<List<Vector2>>> frameSequence = new();
        List<List<Vector2>> dancersInFrame = new();
        List<Vector2> currentPose = new();

        using IDbConnection conn = new SQLiteConnection(connectionString);
        conn.Open();

        List<string> columnNames = new List<string>
        {
            "id", "dancer_id", "camera_id", "frame_id", "position_x", "position_y"
        };

        using IDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = CommandString(columnNames, "cache");

        using IDataReader reader = cmd.ExecuteReader();
        Dictionary<string, int> indexes = ColumnIndexes(reader, columnNames);
        while (reader.Read())
        {
            int frameId = reader.GetInt32(indexes["frame_id"]);
            int dancerId = reader.GetInt32(indexes["dancer_id"]);
            int cameraId = reader.GetInt32(indexes["camera_id"]);

            Vector2 position = new(
                reader.GetFloat(indexes["position_x"]),
                reader.GetFloat(indexes["position_y"]));

            if (cameraId > lastCameraId)
            {
                // next camera
                lastCameraId = cameraId;

                // reset frame and dancer
                frameSequence = new List<List<List<Vector2>>>();
                dancersInFrame = new List<List<Vector2>>();
                currentPose = new List<Vector2>();

                allCameras.Add(frameSequence);
            }

            if (frameId != lastFrameId)
            {
                // next frame
                dancersInFrame = new List<List<Vector2>>();
                currentPose = new List<Vector2>();
                frameSequence.Add(dancersInFrame);

                lastFrameId = frameId;
            }

            if (lastDancerId != dancerId)
            {
                currentPose = new List<Vector2>();
                dancersInFrame.Add(currentPose);

                lastDancerId = dancerId;
            }

            currentPose.Add(position);
        }

        return allCameras;
    }

    static string CommandString(IEnumerable<string> columnNames, string tableName)
    {
        string cmd = columnNames.Aggregate(
            "SELECT ",
            (current, columnName) => current + $"{columnName}, ");

        // remove last comma
        cmd = cmd.Substring(0, cmd.Length - 2) + " ";
        cmd += $"FROM {tableName}";

        return cmd;
    }

    static Dictionary<string, int> ColumnIndexes(IDataRecord reader, IEnumerable<string> columnNames)
    {
        return columnNames
            .ToDictionary(
                columnName => columnName,
                reader.GetOrdinal);
    }
}