import {Entity} from "cs2/bindings";

export interface StationUIUpgradeData {
    Upgrades: StationUIElement[];
}

export interface StationUIElement {
    Entity: Entity;
    Name: string;
}
