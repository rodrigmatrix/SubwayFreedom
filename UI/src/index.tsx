import { ModRegistrar } from "cs2/modding";

import {VanillaComponentResolver} from "./mods/components/vanilla-component/vanilla-components";
import {SubwayStationSelectedInfoComponent} from "./mods/components/StationDetails/SubwayStationSelectedInfoComponent";

const register: ModRegistrar = (moduleRegistry) => {
    VanillaComponentResolver.setRegistry(moduleRegistry);
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', SubwayStationSelectedInfoComponent);
}

export default register;