import React, { useEffect } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import './Modal.css';

const Modal = ({ isOpen, onClose, title, children, footer }) => {
    // Close on Escape key
    useEffect(() => {
        const handleEsc = (e) => {
            if (e.key === 'Escape') onClose();
        };
        if (isOpen) {
            document.addEventListener('keydown', handleEsc);
            document.body.style.overflow = 'hidden'; // Prevent background scrolling
        }
        return () => {
            document.removeEventListener('keydown', handleEsc);
            document.body.style.overflow = 'auto';
        };
    }, [isOpen, onClose]);

    if (!isOpen) return null;

    // Rendered through a portal into <body> — NOT in place.
    //
    // `.modal-overlay` is `position: fixed`, which resolves against the nearest ancestor that
    // establishes a containing block, not necessarily the viewport. Almost every page root carries
    // `animate-fade-in`, whose keyframes end on `transform: translateY(0)` with
    // `animation-fill-mode: forwards` — so that transform persists after the animation and the page
    // root becomes the containing block. The overlay then centred inside the page content (on a long
    // page, e.g. the audit table, that is far below the fold and unreachable) instead of the screen.
    // `.glass-panel`'s `backdrop-filter` has the same effect wherever a modal sits inside one.
    //
    // Portalling to <body> sidesteps every such ancestor, so the modal is always centred on the
    // viewport regardless of what the page above it does. This fixes all Modal call sites at once.
    return createPortal(
        <div className="modal-overlay animate-fade-in" onClick={onClose}>
            <div
                className="modal-content glass-panel"
                onClick={(e) => e.stopPropagation()} // Prevent clicks inside from closing it
            >
                <div className="modal-header">
                    <h2 className="modal-title">{title}</h2>
                    <button className="btn btn-ghost icon-btn small" onClick={onClose}>
                        <X size={20} />
                    </button>
                </div>

                <div className="modal-body">
                    {children}
                </div>

                {footer && (
                    <div className="modal-footer">
                        {footer}
                    </div>
                )}
            </div>
        </div>,
        document.body
    );
};

export default Modal;
