import { NextResponse } from "next/server";
import { workflowRequest } from "@/lib/workflow-api";

export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
    const { id } = await params;
    if (!/^\d+$/.test(id)) return NextResponse.json({ message: "Invalid cycle." }, { status: 400 });
    const { response, result } = await workflowRequest(`/contributioncycles/${id}/close-preview`);
    return response ? NextResponse.json(await response.json()) : NextResponse.json(result, { status: result.status });
}
