import {Tooltip} from "cs2/ui";
import styles from "./SubwayStationSelectedInfoComponent.module.scss";
import classNames from "classnames";
import {useValue} from "cs2/api";
import { getModule } from "cs2/modding";
import { Theme } from "cs2/bindings";
import PickerIcon from "../../../images/Picker.png";
import { FormLine } from "../form-line/form-line";
import { OnOpenPicker } from "../../bindings";
import { VanillaComponentResolver } from "../vanilla-component/vanilla-components";

interface InfoSectionComponent {
	group: string;
	tooltipKeys: Array<string>;
	tooltipTags: Array<string>;
}

const SubwayStationRow = () => {

    return (
		<div>
					<FormLine title={"Select Station Upgrade (Experimental)"}>
						<div style={{ display: 'flex'}}>
							<VanillaComponentResolver.instance.ToolButton
								selected={true}
								multiSelect={false}
								src={PickerIcon}
								tooltip={"Pick Station to Upgrade"}
								className={classNames(
									VanillaComponentResolver.instance.toolButtonTheme.button
								  )}
								onSelect={() => { OnOpenPicker() }}
							/>
						</div>
					</FormLine>

			 </div>
    );
}

export const SubwayStationSelectedInfoComponent = (componentList: any): any => {
	componentList["SubwayFreedomAssetPack.System.SelectedBuildingUISystem"] = (e: InfoSectionComponent) => {
		return <SubwayStationRow />
	}
	return componentList as any;
}
