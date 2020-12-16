using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

using DeveloperConsole;
using GTA;
using GTA.Math;


public class VehicleLabeler : Script
{

    DeveloperConsole.DeveloperConsole developerConsole = null;

    const bool DEBUG = false;

    const int FRAME_LINE_WIDTH = 2;
    Color FRAME_LINE_COLOR = Color.Red;
    const int FRAME_LENGTH_MULTIPLIER = 640;

    const int FRAME_LABEL_OFFSET = 4;
    const float FRAME_LABEL_TEXT_SCALE = .25f;

    const float MAX_Z_DIFF = 4;
    const int MIN_DETECTABLE_FRAME_LENGTH = 48;

    const int FRAME_SAVE_TICK_COUNT = 40;
    int tickCount = 0;

    const string IMAGE_METADATA_HEADER = "frame_position,frame_size,friendly_name,display_name";
    const string PNG_FILE_EXTENSION = ".png";
    const string CSV_FILE_EXTENSION = ".csv";

    const string IMAGES_DIR = "images/";
    const string DEBUG_IMAGES_DIR = "debug_images/";

    public VehicleLabeler()
    {
        this.RegisterConsoleScript(OnConsoleAttached);

        Tick += onTick;
    }

    private void OnConsoleAttached(DeveloperConsole.DeveloperConsole dc)
    {
        developerConsole = dc;
    }

    private void drawVehicleFrame(Point position, Size size)
    {
        Point topLeft = new Point(position.X - size.Width / 2, position.Y - size.Height / 2);
        Point bottomLeft = new Point(position.X - size.Width / 2, position.Y + size.Height / 2);
        Point topRight = new Point(position.X + size.Width / 2, position.Y - size.Height / 2);

        Size horizontalLine = new Size(size.Width, FRAME_LINE_WIDTH);
        Size verticalLine = new Size(FRAME_LINE_WIDTH, size.Height);

        new UIRectangle(topLeft, horizontalLine, FRAME_LINE_COLOR).Draw();
        new UIRectangle(topLeft, verticalLine, FRAME_LINE_COLOR).Draw();
        new UIRectangle(bottomLeft, horizontalLine, FRAME_LINE_COLOR).Draw();
        new UIRectangle(topRight, verticalLine, FRAME_LINE_COLOR).Draw();

        new UIRectangle(position, new Size(2, 2), FRAME_LINE_COLOR).Draw();
    }

    private void drawCalibrationMarker()
    {
        new UIRectangle(new Point(100, 100), new Size(10, 10), FRAME_LINE_COLOR).Draw();
    }

    private string saveScreenToFile()
    {
        string uuid = Guid.NewGuid().ToString();
        Rectangle bounds = Screen.GetBounds(Point.Empty);
        using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
            if (DEBUG)
            {
                bitmap.Save(DEBUG_IMAGES_DIR + uuid + PNG_FILE_EXTENSION, ImageFormat.Png);
            } else
            {
                bitmap.Save(IMAGES_DIR + uuid + PNG_FILE_EXTENSION, ImageFormat.Png);
            }
        }
        return uuid;
    }

    private void saveImageMetadata(string imageUUID, List<Point> framePositions, List<Size> frameSizes, List<string> vehicleFriendlyNames, List<string> vehicleDisplayNames)
    {
        string fileStr = IMAGE_METADATA_HEADER + "\n";
        for (int i = 0; i < framePositions.Count; i++)
        {
            fileStr += framePositions[i].X.ToString() + " " + framePositions[i].Y.ToString() + ",";
            fileStr += frameSizes[i].Width.ToString() + " " + frameSizes[i].Width.ToString() + ",";
            fileStr += vehicleFriendlyNames[i] + "," + vehicleDisplayNames[i] +"\n";
        }
        if (DEBUG)
        {
            System.IO.File.WriteAllText(DEBUG_IMAGES_DIR + imageUUID + CSV_FILE_EXTENSION, fileStr);
        } else
        {
            System.IO.File.WriteAllText(IMAGES_DIR + imageUUID + CSV_FILE_EXTENSION, fileStr);
        }
    }

    private void processEntities()
    {
        if (tickCount == FRAME_SAVE_TICK_COUNT)
        {
            tickCount = 0;

            List<Point> framePositions = new List<Point>();
            List<Size> frameSizes = new List<Size>();
            List<string> vehicleFriendlyNames = new List<string>();
            List<string> vehicleDisplayNames = new List<string>();
            foreach (var entity in World.GetAllEntities())
            {
                if (GTAFuncs.GetEntityType(entity) != GTAFuncs.EntityType.Vehicle) continue;
                if (!entity.IsOnScreen || entity.IsOccluded) continue;

                Vector2 entityScreenPosition = GTAFuncs.WorldToScreen(entity.Position);

                if (Math.Abs(Game.Player.Character.Position.Z - entity.Position.Z) > MAX_Z_DIFF) continue;

                float playerEntityDistance = Game.Player.Character.Position.DistanceTo2D(entity.Position);
                int frameLength = (int)(entity.Model.GetDimensions().Y * FRAME_LENGTH_MULTIPLIER * (1 / playerEntityDistance));

                if (frameLength < MIN_DETECTABLE_FRAME_LENGTH) continue;

                Vehicle vehicle = new Vehicle(entity.Handle);
                Point labelPosition = new Point((int)entityScreenPosition.X, (int)entityScreenPosition.Y + frameLength + FRAME_LABEL_OFFSET);
                UIText frameLabel = new UIText(vehicle.FriendlyName, labelPosition, FRAME_LABEL_TEXT_SCALE);

                Point framePosition = new Point((int)entityScreenPosition.X, (int)entityScreenPosition.Y);
                Size frameSize = new Size(frameLength, frameLength);

                framePositions.Add(framePosition);
                frameSizes.Add(frameSize);
                vehicleFriendlyNames.Add(vehicle.FriendlyName);
                vehicleDisplayNames.Add(vehicle.DisplayName);
            }

            string imageUUID = saveScreenToFile();
            saveImageMetadata(imageUUID, framePositions, frameSizes, vehicleFriendlyNames, vehicleDisplayNames);
        }
        else if (DEBUG)
        {
            List<Point> framePositions = new List<Point>();
            List<Size> frameSizes = new List<Size>();
            List<string> vehicleFriendlyNames = new List<string>();
            List<string> vehicleDisplayNames = new List<string>();
            foreach (var entity in World.GetAllEntities())
            {
                if (GTAFuncs.GetEntityType(entity) != GTAFuncs.EntityType.Vehicle) continue;
                if (!entity.IsOnScreen || entity.IsOccluded) continue;

                Vector2 entityScreenPosition = GTAFuncs.WorldToScreen(entity.Position);

                if (Math.Abs(Game.Player.Character.Position.Z - entity.Position.Z) > MAX_Z_DIFF) continue;

                float playerEntityDistance = Game.Player.Character.Position.DistanceTo2D(entity.Position);
                int frameLength = (int)(entity.Model.GetDimensions().Y * FRAME_LENGTH_MULTIPLIER * (1 / playerEntityDistance));

                if (frameLength < MIN_DETECTABLE_FRAME_LENGTH) continue;

                Vehicle vehicle = new Vehicle(entity.Handle);
                Point labelPosition = new Point((int)entityScreenPosition.X, (int)entityScreenPosition.Y + frameLength + FRAME_LABEL_OFFSET);
                UIText frameLabel = new UIText(vehicle.FriendlyName, labelPosition, FRAME_LABEL_TEXT_SCALE);

                Point framePosition = new Point((int)entityScreenPosition.X, (int)entityScreenPosition.Y);
                Size frameSize = new Size(frameLength, frameLength);

                framePositions.Add(framePosition);
                frameSizes.Add(frameSize);
                vehicleFriendlyNames.Add(vehicle.FriendlyName);
                vehicleDisplayNames.Add(vehicle.DisplayName);
            }

            for (int i = 0; i < framePositions.Count; i++)
            {
                drawVehicleFrame(framePositions[i], frameSizes[i]);
            }

            drawCalibrationMarker();
        }
    }

    private void onTick(object sender, EventArgs e)
    {
        tickCount += 1;
        processEntities();
    }
}