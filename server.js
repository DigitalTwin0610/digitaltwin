/**
 * EmoLamp Server
 * 
 * MQTT 스타일 메시지 브로커 서버
 * Unity와 외부 클라이언트 간 실시간 통신 지원
 * 
 * 엔드포인트:
 * - GET  /api/status    : 서버 상태 확인
 * - POST /api/publish   : 메시지 발행
 * - GET  /api/poll      : 메시지 폴링 (Long Polling)
 * - GET  /api/state     : 현재 램프 상태 조회
 * - POST /api/state     : 램프 상태 업데이트
 */

require('dotenv').config();
const express = require('express');
const cors = require('cors');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;

// ==================== 미들웨어 ====================
app.use(cors());
app.use(express.json());

// 정적 파일 서빙 (public 폴더)
app.use(express.static(path.join(__dirname, 'public')));

// 요청 로깅
app.use((req, res, next) => {
    const timestamp = new Date().toISOString();
    console.log(`[${timestamp}] ${req.method} ${req.path}`);
    next();
});

// ==================== 데이터 저장소 ====================
// 메시지 큐 (토픽별)
const messageQueues = new Map();

// 현재 램프 상태
let lampState = {
    mode: 'AUTO',
    emotion: 'calm',
    hue: 120,
    saturation: 70,
    brightness: 70,
    colorHex: '#50C878',
    manualColorHex: '#FFFFFF',
    summary: '',
    weather: '',
    timestamp: Date.now()
};

// 구독자 목록 (클라이언트별 마지막 폴링 시간)
const subscribers = new Map();

// ==================== 유틸리티 ====================
function addMessage(topic, payload, clientId = 'server') {
    const message = {
        topic,
        payload,
        clientId,
        timestamp: Date.now()
    };

    if (!messageQueues.has(topic)) {
        messageQueues.set(topic, []);
    }

    const queue = messageQueues.get(topic);
    queue.push(message);

    // 최대 100개 메시지 유지
    if (queue.length > 100) {
        queue.shift();
    }

    console.log(`[PUBLISH] ${topic}: ${JSON.stringify(payload).substring(0, 100)}`);
    return message;
}

function getMessagesSince(since, clientId) {
    const messages = [];

    for (const [topic, queue] of messageQueues) {
        for (const msg of queue) {
            // 자신이 보낸 메시지는 제외, since 이후 메시지만
            if (msg.timestamp > since && msg.clientId !== clientId) {
                messages.push(msg);
            }
        }
    }

    // 시간순 정렬
    messages.sort((a, b) => a.timestamp - b.timestamp);
    return messages;
}

// ==================== API 엔드포인트 ====================

// 서버 상태 확인
app.get('/api/status', (req, res) => {
    res.json({
        status: 'ok',
        server: 'EmoLamp Server',
        version: '1.0.0',
        uptime: process.uptime(),
        timestamp: Date.now()
    });
});

// 메시지 발행
app.post('/api/publish', (req, res) => {
    try {
        const { topic, payload, clientId } = req.body;

        if (!topic) {
            return res.status(400).json({ error: 'topic is required' });
        }

        const message = addMessage(topic, payload, clientId || 'anonymous');

        // 특정 토픽은 상태 업데이트
        if (topic === 'emolamp/state' && payload) {
            Object.assign(lampState, payload, { timestamp: Date.now() });
        }

        res.json({
            success: true,
            message
        });
    } catch (error) {
        console.error('[ERROR] publish:', error);
        res.status(500).json({ error: error.message });
    }
});

// 메시지 폴링 (Long Polling)
app.get('/api/poll', (req, res) => {
    try {
        const clientId = req.query.clientId || 'anonymous';
        const since = parseInt(req.query.since) || 0;

        // 구독자 등록/업데이트
        subscribers.set(clientId, Date.now());

        const messages = getMessagesSince(since, clientId);

        if (messages.length > 0) {
            // 가장 최근 메시지 반환
            res.json(messages[messages.length - 1]);
        } else {
            res.json(null);
        }
    } catch (error) {
        console.error('[ERROR] poll:', error);
        res.status(500).json({ error: error.message });
    }
});

// 현재 상태 조회
app.get('/api/state', (req, res) => {
    res.json(lampState);
});

// 상태 업데이트
app.post('/api/state', (req, res) => {
    try {
        const newState = req.body;

        if (newState) {
            Object.assign(lampState, newState, { timestamp: Date.now() });

            // 상태 변경을 메시지로도 발행
            addMessage('emolamp/state', lampState, 'server');

            console.log(`[STATE] Updated: mode=${lampState.mode}, emotion=${lampState.emotion}`);
        }

        res.json({
            success: true,
            state: lampState
        });
    } catch (error) {
        console.error('[ERROR] state:', error);
        res.status(500).json({ error: error.message });
    }
});

// LED 색상 직접 제어
app.post('/api/led', (req, res) => {
    try {
        const { r, g, b } = req.body;

        const ledPayload = { r, g, b };
        addMessage('emolamp/led', ledPayload, 'api');

        res.json({
            success: true,
            led: ledPayload
        });
    } catch (error) {
        console.error('[ERROR] led:', error);
        res.status(500).json({ error: error.message });
    }
});

// 헬스체크 (Cloudtype용)
app.get('/healthz', (req, res) => {
    res.status(200).send('OK');
});

// 루트 경로
app.get('/', (req, res) => {
    res.json({
        name: 'EmoLamp Server',
        description: 'MQTT-style IoT Server for EmoLamp',
        endpoints: {
            status: 'GET /api/status',
            publish: 'POST /api/publish',
            poll: 'GET /api/poll?clientId=xxx&since=timestamp',
            state: 'GET|POST /api/state',
            led: 'POST /api/led'
        },
        currentState: lampState
    });
});

// 404 처리
app.use((req, res) => {
    res.status(404).json({ error: 'Not Found' });
});

// 에러 처리
app.use((err, req, res, next) => {
    console.error('[ERROR]', err);
    res.status(500).json({ error: err.message });
});

// ==================== 서버 시작 ====================
app.listen(PORT, () => {
    console.log('========================================');
    console.log(`  EmoLamp Server v1.0.0`);
    console.log(`  Running on port ${PORT}`);
    console.log(`  http://localhost:${PORT}`);
    console.log('========================================');
});

// ==================== 정리 작업 ====================
// 오래된 구독자 정리 (5분마다)
setInterval(() => {
    const now = Date.now();
    const timeout = 5 * 60 * 1000; // 5분

    for (const [clientId, lastPoll] of subscribers) {
        if (now - lastPoll > timeout) {
            subscribers.delete(clientId);
            console.log(`[CLEANUP] Removed inactive subscriber: ${clientId}`);
        }
    }
}, 60 * 1000);

// 오래된 메시지 정리 (1시간마다)
setInterval(() => {
    const now = Date.now();
    const maxAge = 60 * 60 * 1000; // 1시간

    for (const [topic, queue] of messageQueues) {
        const filtered = queue.filter(msg => now - msg.timestamp < maxAge);
        messageQueues.set(topic, filtered);
    }

    console.log('[CLEANUP] Old messages cleaned');
}, 60 * 60 * 1000);
