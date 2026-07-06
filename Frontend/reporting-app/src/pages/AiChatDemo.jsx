import React, { useState, useRef, useEffect } from 'react';
import { Bot, User, Send, Loader2, ShieldAlert, RotateCcw, Wrench } from 'lucide-react';
import { AiChatService } from '../services/aiChatService';
import {
    initialChatState, applyResponse, buildMessageRequest, buildConfirmRequest,
} from '../utils/chatSession';
import './AiChatDemo.css';

// Pull the display text out of a ChatMessageDTO's content blocks (camelCase wire shape).
const messageText = (msg) =>
    (msg.content || []).filter((c) => c.type === 'text' && c.text).map((c) => c.text).join('\n');

// The tool_use blocks in an assistant turn — rendered as subtle chips so the demo shows the agent
// actually driving gateway tool calls (the thing the audit log then records).
const toolUses = (msg) => (msg.content || []).filter((c) => c.type === 'tool_use');

const AiChatDemo = () => {
    const [session, setSession] = useState(initialChatState);
    const [input, setInput] = useState('');
    const [pendingUserText, setPendingUserText] = useState(null); // optimistic in-flight bubble
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const scrollRef = useRef(null);

    useEffect(() => {
        scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' });
    }, [session, loading]);

    const run = async (buildRequest, optimisticText) => {
        setError(null);
        setLoading(true);
        if (optimisticText) setPendingUserText(optimisticText);
        try {
            const res = await AiChatService.send(buildRequest(session));
            setSession((s) => applyResponse(s, res));
        } catch (err) {
            console.error('AiChat error:', err);
            setError(err.message || 'Asistent trenutno ne može da obradi zahtev.');
        } finally {
            setLoading(false);
            setPendingUserText(null);
        }
    };

    const sendMessage = (e) => {
        e.preventDefault();
        const text = input.trim();
        if (!text || loading || session.pending) return;
        setInput('');
        run((s) => buildMessageRequest(s, text), text);
    };

    const decide = (approved) => {
        if (loading || !session.pending) return;
        run((s) => buildConfirmRequest(s, approved), null);
    };

    const reset = () => {
        setSession(initialChatState());
        setInput(''); setError(null); setPendingUserText(null);
    };

    const isEmpty = session.history.length === 0 && !pendingUserText && !loading;

    return (
        <div className="animate-fade-in aichat-page">
            <div className="page-header">
                <div>
                    <h1 className="page-title">
                        <Bot size={28} style={{ color: 'var(--accent-primary)' }} /> AI Assistant
                    </h1>
                    <p className="page-description">
                        Demo agent. Read upiti se izvršavaju odmah kroz gateway; svaka izmena (write) traži
                        vašu potvrdu pre izvršenja. Svaki poziv gateway autorizuje i beleži u audit.
                    </p>
                </div>
                <button className="btn btn-ghost" onClick={reset} title="Novi razgovor">
                    <RotateCcw size={16} /> Novi razgovor
                </button>
            </div>

            <div className="glass-panel aichat-window">
                <div className="aichat-messages" ref={scrollRef}>
                    {isEmpty && (
                        <div className="empty-state aichat-empty">
                            Postavite pitanje, npr. „Koliko aktivnih kutija ima moja organizacija?“
                        </div>
                    )}

                    {session.history.map((msg, i) => {
                        const text = messageText(msg);
                        const tools = msg.role === 'assistant' ? toolUses(msg) : [];
                        if (!text && tools.length === 0) return null;
                        const mine = msg.role === 'user';
                        return (
                            <div key={i} className={`aichat-msg ${mine ? 'mine' : 'theirs'}`}>
                                <div className="aichat-avatar">{mine ? <User size={16} /> : <Bot size={16} />}</div>
                                <div className="aichat-bubble">
                                    {text && <div className="aichat-text">{text}</div>}
                                    {tools.map((t) => (
                                        <div key={t.toolUseId} className="aichat-tool">
                                            <Wrench size={13} /> <code>{t.toolName}</code>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        );
                    })}

                    {pendingUserText && (
                        <div className="aichat-msg mine">
                            <div className="aichat-avatar"><User size={16} /></div>
                            <div className="aichat-bubble"><div className="aichat-text">{pendingUserText}</div></div>
                        </div>
                    )}

                    {loading && (
                        <div className="aichat-msg theirs">
                            <div className="aichat-avatar"><Bot size={16} /></div>
                            <div className="aichat-bubble aichat-thinking">
                                <Loader2 size={16} style={{ animation: 'spin 1s linear infinite' }} /> Asistent razmišlja…
                            </div>
                        </div>
                    )}

                    {error && (
                        <div className="aichat-error">
                            <ShieldAlert size={16} /> <span>{error}</span>
                        </div>
                    )}
                </div>

                {/* Propose-confirm HITL: a proposed write must be explicitly approved/rejected. */}
                {session.pending && !loading && (
                    <div className="aichat-proposal">
                        <div className="aichat-proposal-head">
                            <ShieldAlert size={18} /> Asistent predlaže izmenu — potrebna je vaša potvrda
                        </div>
                        <div className="aichat-proposal-body">
                            <div><span className="aichat-proposal-label">Alat:</span> <code>{session.pending.toolName}</code></div>
                            <div><span className="aichat-proposal-label">Argumenti:</span> <code>{session.pending.argsSummary}</code></div>
                        </div>
                        <div className="aichat-proposal-actions">
                            <button className="btn btn-primary" onClick={() => decide(true)}>Odobri i izvrši</button>
                            <button className="btn btn-ghost" onClick={() => decide(false)}>Odbij</button>
                        </div>
                    </div>
                )}

                <form className="aichat-input" onSubmit={sendMessage}>
                    <input
                        className="form-control"
                        placeholder={session.pending ? 'Prvo odlučite o predlogu iznad…' : 'Pitajte asistenta…'}
                        value={input}
                        onChange={(e) => setInput(e.target.value)}
                        disabled={loading || !!session.pending}
                    />
                    <button className="btn btn-primary" type="submit"
                        disabled={loading || !!session.pending || !input.trim()}>
                        <Send size={16} /> Pošalji
                    </button>
                </form>
            </div>
        </div>
    );
};

export default AiChatDemo;
