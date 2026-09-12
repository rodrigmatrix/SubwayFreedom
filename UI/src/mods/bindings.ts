import {bindValue, trigger} from "cs2/api";
import {Entity} from "cs2/bindings";
import mod from "mod.json";

export const OnOpenPicker = () => trigger(mod.id, "OnOpenPicker");
