import Link from "next/link";

export default function PaymentProofLink({ stokvelId, cycleId, memberId, userId, role }: {
    stokvelId: number; cycleId: number; memberId: number; userId: number; role: string;
}) {
    if (memberId !== userId && role !== "Treasurer") return null;
    return <Link className="text-sm underline" href={`/stokvels/${stokvelId}/proof?cycle=${cycleId}&member=${memberId}`}>Proof of payment</Link>;
}
