// Developed by Klyte45
import { Component } from "react";
import styles from './form-line-style.module.scss'


export class FormLine extends Component<{
    title: string | null | JSX.Element;
    onClick?: () => void;
    compact?: boolean
    className?: string
    subtitle?: string | JSX.Element
    children?: JSX.Element | JSX.Element[] |string
}, {}> {
    constructor({props}: { props: any }) {
        super(props);
        this.state = {};
    }
    render() {
        return (
            <>
                <div className={[styles.cs2FieldStyle, (this.props.compact ? styles.cs2FieldStyle.compact : styles.cs2FieldStyle), this.props.className ?? ""].join(" ")} onClick={() => this.props.onClick?.()}>
                    <div className={[styles.cs2FormItemLabel, styles.cs2FormItemLabel2].join(" ")}>
                        {this.props.title}
                        {this.props.subtitle}
                    </div>
                    {this.props.children}
                </div>
            </>
        );
    }
}