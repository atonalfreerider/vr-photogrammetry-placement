#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using UnityEngine;

namespace Pose
{
    /// <summary>
    /// Sqlite db needs to have a frames table
    /// </summary>
    public class SqliteInput : MonoBehaviour
    {
        public class DbFigure
        {
            public int Role;
            public readonly List<List<Vector2>?> PosesByFrame = new();

            public DbFigure(int role)
            {
                Role = role;
            }
        }

        [Header("Input")] public string DbPath;

        public static int FrameMax = -1;

        public List<List<List<List<Vector2>>>> ReadFrameFromDb()
        {
            List<List<List<List<Vector2>>>> allCameras = new();

            string connectionString = "URI=file:" + DbPath;

            int lastCameraId = -1;
            int lastFrameId = -1;
            int poseCounter = -1;

            List<List<List<Vector2>>> frameSequence = new();
            List<List<Vector2>> figuresInFrame = new();
            List<Vector2> currentPose = new();

            using IDbConnection conn = new SQLiteConnection(connectionString);
            conn.Open();

            List<string> columnNames = new List<string>
            {
                "id", "camera_id", "frame_id", "position_x", "position_y"
            };

            using IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = CommandString(columnNames, "cache_poses");

            using IDataReader reader = cmd.ExecuteReader();
            Dictionary<string, int> indexes = ColumnIndexes(reader, columnNames);
            int numPoses = Enum.GetNames(typeof(Joints)).Length;
            while (reader.Read())
            {
                int frameId = reader.GetInt32(indexes["frame_id"]);
                int cameraId = reader.GetInt32(indexes["camera_id"]);
                poseCounter++;

                if (frameId > FrameMax)
                {
                    FrameMax = frameId;
                }

                Vector2 position = new(
                    reader.GetFloat(indexes["position_x"]),
                    -reader.GetFloat(indexes["position_y"]));

                if (cameraId > lastCameraId)
                {
                    // next camera
                    lastCameraId = cameraId;

                    // reset frame and figure
                    frameSequence = new List<List<List<Vector2>>>();
                    figuresInFrame = new List<List<Vector2>>();
                    currentPose = new List<Vector2>();

                    allCameras.Add(frameSequence);
                }

                if (frameId != lastFrameId)
                {
                    // next frame
                    figuresInFrame = new List<List<Vector2>>();
                    currentPose = new List<Vector2>();
                    frameSequence.Add(figuresInFrame);

                    lastFrameId = frameId;
                }

                if (poseCounter % numPoses == 0)
                {
                    figuresInFrame.Add(currentPose);
                    currentPose = new List<Vector2>();
                }

                currentPose.Add(position);
            }

            return allCameras;
        }

        public List<DbFigure> ReadFiguresFromAllCameras(int role)
        {
            List<DbFigure> figuresByCamera = new();

            string connectionString = "URI=file:" + DbPath;

            DbFigure currentFigure = new DbFigure(role);

            int lastCameraId = -1;
            int lastFrameId = -1;
            List<Vector2> currentPose = new();

            using IDbConnection conn = new SQLiteConnection(connectionString);
            conn.Open();

            List<string> columnNames = new List<string>
            {
                "id", "camera_id", "frame_id", "position_x", "position_y"
            };

            string tableName = role switch
            {
                0 => "lead",
                1 => "follow",
                _ => ""
            };

            using IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = CommandString(columnNames, tableName);

            using IDataReader reader = cmd.ExecuteReader();
            Dictionary<string, int> indexes = ColumnIndexes(reader, columnNames);
            while (reader.Read())
            {
                int frameId = reader.GetInt32(indexes["frame_id"]);
                int cameraId = reader.GetInt32(indexes["camera_id"]);

                if (frameId > FrameMax) FrameMax = frameId;

            

                if (cameraId > lastCameraId)
                {
                    if (lastCameraId > -1)
                    {
                        currentFigure.PosesByFrame.Add(currentPose.Any()
                            ? currentPose
                            : null);
                    }

                    lastCameraId = cameraId;
                    currentFigure = new DbFigure(role);

                    currentPose = new List<Vector2>();

                    figuresByCamera.Add(currentFigure);
                }

                if (frameId != lastFrameId)
                {
                    if (frameId > 0)
                    {
                        currentFigure.PosesByFrame.Add(currentPose.Any()
                            ? currentPose
                            : null);
                    }

                    currentPose = new List<Vector2>();

                    lastFrameId = frameId;
                }

                int? x = reader.IsDBNull(indexes["position_x"]) ? null : reader.GetInt32(indexes["position_x"]);
                int? y = reader.IsDBNull(indexes["position_y"]) ? null : -reader.GetInt32(indexes["position_y"]);
            
                if (x.HasValue && y.HasValue)
                {
                    Vector2 point = new Vector2(x.Value, y.Value);
                    currentPose.Add(point);
                }
            }

            return figuresByCamera;
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
}