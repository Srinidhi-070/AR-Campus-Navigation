[System.Serializable]
public class LocationData
{
    public string   id;
    public string   displayName;
    public string   type;
    public string   building;
    public int      floor;
    public float    x;
    public float    y;
    public float    z;
    public float    rotation_y;
    public bool     qr_point;
    public string   description;
    public string[] neighbors;
}
