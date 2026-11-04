namespace RondiTrack.Models;

// Private database bytes: never published through static files or storage URLs.
public class PaymentProof
{
    public int StokvelId { get; set; }
    public int UserId { get; set; }
    public int Cycle { get; set; }
    public int UploaderId { get; set; }
    public byte[] Bytes { get; set; } = [];
    public string ContentType { get; set; } = "";
}

public class CycleCloseReceipt
{
    public int CycleId { get; set; }
    public int UserId { get; set; }
    public string Key { get; set; } = "";
    public string Version { get; set; } = "";
    public string ResultJson { get; set; } = "";
}
