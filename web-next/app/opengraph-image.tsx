import { ImageResponse } from "next/og";

export const alt = "RondiTrack — stokvel contribution management";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default function Image() {
    return new ImageResponse(<div style={{ display: "flex", flexDirection: "column", justifyContent: "center", background: "#10213a", color: "white", width: "100%", height: "100%", padding: 80 }}>
        <div style={{ fontSize: 90 }}>RondiTrack</div>
        <div style={{ fontSize: 40, marginTop: 30 }}>Stokvel contribution management</div>
    </div>, size);
}
