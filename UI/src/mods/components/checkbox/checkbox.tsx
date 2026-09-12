import { FormLine } from "../form-line/form-line";
import styles from '../form-line/form-line-style.module.scss'
import { Icon } from "cs2/ui";
import checkIcon from "../../../images/Checkmark.svg";

export interface Cs2CheckboxProps {
    title: string | null;
    isChecked: boolean;
    onValueToggle: (newVal: boolean) => void;
}

export const CheckBoxWithLine = (props: Cs2CheckboxProps) => {
    const { title, onValueToggle } = props;
    const isChecked = props.isChecked;
    return (
        <FormLine title={title}>
            <Cs2Checkbox isChecked={props.isChecked} onValueToggle={(x) => onValueToggle(x)} />
        </FormLine>
    );
}

interface Cs2CheckboxTitleLessProps {
    isChecked: boolean;
    onValueToggle: (newVal: boolean) => void;
}

export const Cs2Checkbox = (props: Cs2CheckboxTitleLessProps) => {
    const { onValueToggle } = props;
    const isChecked = props.isChecked;

    return (<>
        <div className={[styles.cs2Toggle, styles.cs2ItemMouseStates].join(" ")} onClick={() => onValueToggle(!isChecked)}>
            { isChecked ?
                <Icon
                    src={checkIcon}
                    className={styles.cs2CheckmarkIcon}
                    tinted={true}/> : null
            }
        </div>
    </>);
}