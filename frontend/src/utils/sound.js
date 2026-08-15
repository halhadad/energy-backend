// shared audio context for the alert beep.
// the browser blocks audio until a user gesture, so primeAudio resumes it on the first
// click or keypress, after that a beep fired from a signalr push is allowed to play

let ctx = null;
let primed = false;

function getCtx() {
  if (!ctx) {
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return null;
    ctx = new AC();
  }
  return ctx;
}

export function primeAudio() {
  if (primed) return;
  primed = true;

  const resume = () => {
    const c = getCtx();
    if (c && c.state === "suspended") c.resume().catch(() => {});
    window.removeEventListener("pointerdown", resume);
    window.removeEventListener("keydown", resume);
  };

  window.addEventListener("pointerdown", resume);
  window.addEventListener("keydown", resume);
}

export function playAlertBeep() {
  try {
    const c = getCtx();
    if (!c) return;
    if (c.state === "suspended") c.resume().catch(() => {});

    const osc = c.createOscillator();
    const gain = c.createGain();
    osc.connect(gain);
    gain.connect(c.destination);

    const t = c.currentTime;
    osc.type = "sine";
    osc.frequency.setValueAtTime(880, t);
    // quick fade in and out so it doesn't click
    gain.gain.setValueAtTime(0.0001, t);
    gain.gain.exponentialRampToValueAtTime(0.4, t + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.5);

    osc.start(t);
    osc.stop(t + 0.5);
  } catch {
    // blocked or not supported, just stay silent
  }
}
