import type { HomeStokvel } from "./api";

export function getTreasurerStokvels(
    stokvels: HomeStokvel[]
): HomeStokvel[] {
    return stokvels.filter((stokvel) => stokvel.role === "Treasurer");
}