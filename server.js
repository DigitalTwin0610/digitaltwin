/**
 * EmoLamp Statistics Server
 * 
 * 감정 로그 수집 및 실시간 통계 대시보드 서버
 * Unity에서 전송한 데이터를 수집하고 시각화
 * 
 * 엔드포인트:
 * - POST /api/log/emotion   : 감정 분석 로그 저장
 * - POST /api/log/weather   : 날씨 정보 로그 저장
 * - POST /api/log/state     : 상태 변경 로그 저장
 * - GET  /api/stats/today   : 오늘 통계
 * - GET  /api/stats/emotions: 감정 분포
 * - GET  /api/stats/timeline: 타임라인 데이터
 * - GET  /api/stats/summary : 전체 요약
 * - GET  /api/stats/recent  : 최근 로그
 * - GET  /api/status        : 서버 상태
 * - GET  /                  : 대시보드 HTML
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

// 정적 파일 서빙
app.use(express.static(path.join(__dirname, 'public')));

// 요청 로깅
app.use((req, res, next) => {
    const timestamp = new Date().toISOString();
    console.log(`[${timestamp}] ${req.method} ${req.path}`);
    next();
});

// ==================== 데이터 저장소 ====================
// 감정 로그
const emotionLogs = [];

// 날씨 로그
const weatherLogs = [];

// 상태 변경 로그
const stateLogs = [];

// 현재 상태 (최신)
let currentState = {
    emotion: 'calm',
    hue: 120,
    saturation: 70,
    brightness: 70,
    colorHex: '#50C878',
    mode: 'AUTO',
    weather: null,
    temperature: null,
    timestamp: Date.now()
};

// 최대 로그 수
const MAX_LOGS = 1000;

// ==================== 유틸리티 ====================
function getToday() {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
}

function getKoreanTime(timestamp) {
    const date = new Date(timestamp);
    return date.toLocaleString('ko-KR', { timeZone: 'Asia/Seoul' });
}

function getHourFromTimestamp(timestamp) {
    const date = new Date(timestamp);
    return date.getHours();
}

function addLog(logs, entry) {
    logs.push(entry);
    if (logs.length > MAX_LOGS) {
        logs.shift();
    }
}

function getEmotionEmoji(emotion) {
    const emojis = {
        joy: '😊',
        sadness: '😢',
        anger: '😡',
        calm: '😌',
        excited: '🤩',
        fear: '😨',
        surprise: '😲'
    };
    return emojis[emotion] || '😐';
}

function getEmotionKorean(emotion) {
    const korean = {
        joy: '기쁨',
        sadness: '슬픔',
        anger: '분노',
        calm: '평온',
        excited: '설렘',
        fear: '두려움',
        surprise: '놀람'
    };
    return korean[emotion] || '중립';
}

function getWeatherEmoji(condition) {
    const emojis = {
        Clear: '☀️',
        Clouds: '☁️',
        Overcast: '🌥️',
        Rain: '🌧️',
        Snow: '❄️',
        Fog: '🌫️',
        Storm: '⛈️'
    };
    return emojis[condition] || '🌤️';
}

// ==================== 로그 수집 API ====================

// 감정 분석 로그 저장
app.post('/api/log/emotion', (req, res) => {
    try {
        const { emotion, hue, saturation, brightness, summary, colorHex } = req.body;
        
        const entry = {
            emotion: emotion || 'calm',
            hue: hue || 120,
            saturation: saturation || 70,
            brightness: brightness || 70,
            summary: summary || '',
            colorHex: colorHex || '#50C878',
            emoji: getEmotionEmoji(emotion),
            emotionKorean: getEmotionKorean(emotion),
            timestamp: Date.now()
        };
        
        addLog(emotionLogs, entry);
        
        // 현재 상태 업데이트
        Object.assign(currentState, {
            emotion: entry.emotion,
            hue: entry.hue,
            saturation: entry.saturation,
            brightness: entry.brightness,
            colorHex: entry.colorHex,
            timestamp: entry.timestamp
        });
        
        console.log(`[EMOTION] ${entry.emoji} ${entry.emotionKorean} (H:${entry.hue})`);
        
        res.json({ success: true, entry });
    } catch (error) {
        console.error('[ERROR] log/emotion:', error);
        res.status(500).json({ error: error.message });
    }
});

// 날씨 정보 로그 저장
app.post('/api/log/weather', (req, res) => {
    try {
        const { temperature, humidity, condition, description, cityName } = req.body;
        
        const entry = {
            temperature: temperature || 0,
            humidity: humidity || 0,
            condition: condition || 'Clouds',
            description: description || '',
            cityName: cityName || 'Seoul',
            emoji: getWeatherEmoji(condition),
            timestamp: Date.now()
        };
        
        addLog(weatherLogs, entry);
        
        // 현재 상태 업데이트
        currentState.weather = entry.condition;
        currentState.temperature = entry.temperature;
        
        console.log(`[WEATHER] ${entry.emoji} ${entry.temperature}°C ${entry.condition}`);
        
        res.json({ success: true, entry });
    } catch (error) {
        console.error('[ERROR] log/weather:', error);
        res.status(500).json({ error: error.message });
    }
});

// 상태 변경 로그 저장
app.post('/api/log/state', (req, res) => {
    try {
        const { mode, action, details } = req.body;
        
        const entry = {
            mode: mode || 'AUTO',
            action: action || 'unknown',
            details: details || {},
            timestamp: Date.now()
        };
        
        addLog(stateLogs, entry);
        
        // 현재 상태 업데이트
        currentState.mode = entry.mode;
        
        console.log(`[STATE] ${entry.action} (mode: ${entry.mode})`);
        
        res.json({ success: true, entry });
    } catch (error) {
        console.error('[ERROR] log/state:', error);
        res.status(500).json({ error: error.message });
    }
});

// Manual 상태 실시간 업데이트
app.post('/api/log/manualstate', (req, res) => {
    try {
        const { mode, hue, saturation, brightness, colorHex } = req.body;
        
        currentState.mode = mode || 'MANUAL';
        currentState.hue = hue || 0;
        currentState.saturation = saturation || 0;
        currentState.brightness = brightness || 0;
        currentState.colorHex = colorHex || '#FFFFFF';
        currentState.timestamp = Date.now();
        
        res.json({ success: true });
    } catch (error) {
        res.status(500).json({ error: error.message });
    }
});

// ==================== 통계 API ====================

// 오늘 통계
app.get('/api/stats/today', (req, res) => {
    try {
        const todayStart = getToday();
        const todayLogs = emotionLogs.filter(log => log.timestamp >= todayStart);
        
        const emotionCounts = {};
        todayLogs.forEach(log => {
            emotionCounts[log.emotion] = (emotionCounts[log.emotion] || 0) + 1;
        });
        
        // 가장 많은 감정
        let dominantEmotion = 'calm';
        let maxCount = 0;
        for (const [emotion, count] of Object.entries(emotionCounts)) {
            if (count > maxCount) {
                maxCount = count;
                dominantEmotion = emotion;
            }
        }
        
        res.json({
            count: todayLogs.length,
            emotionCounts,
            dominantEmotion,
            dominantEmoji: getEmotionEmoji(dominantEmotion),
            dominantKorean: getEmotionKorean(dominantEmotion)
        });
    } catch (error) {
        console.error('[ERROR] stats/today:', error);
        res.status(500).json({ error: error.message });
    }
});

// 감정 분포
app.get('/api/stats/emotions', (req, res) => {
    try {
        const counts = {};
        const total = emotionLogs.length;
        
        emotionLogs.forEach(log => {
            counts[log.emotion] = (counts[log.emotion] || 0) + 1;
        });
        
        const distribution = Object.entries(counts).map(([emotion, count]) => ({
            emotion,
            emoji: getEmotionEmoji(emotion),
            korean: getEmotionKorean(emotion),
            count,
            percentage: total > 0 ? Math.round((count / total) * 100) : 0
        }));
        
        // 비율 순 정렬
        distribution.sort((a, b) => b.count - a.count);
        
        res.json({
            total,
            distribution
        });
    } catch (error) {
        console.error('[ERROR] stats/emotions:', error);
        res.status(500).json({ error: error.message });
    }
});

// 타임라인 데이터 (오늘 시간대별)
app.get('/api/stats/timeline', (req, res) => {
    try {
        const todayStart = getToday();
        const todayLogs = emotionLogs.filter(log => log.timestamp >= todayStart);
        
        // 시간대별 그룹화
        const hourlyData = {};
        for (let i = 0; i < 24; i++) {
            hourlyData[i] = [];
        }
        
        todayLogs.forEach(log => {
            const hour = getHourFromTimestamp(log.timestamp);
            hourlyData[hour].push(log);
        });
        
        // 시간대별 대표 감정 결정
        const timeline = [];
        for (let hour = 0; hour < 24; hour++) {
            const logs = hourlyData[hour];
            if (logs.length > 0) {
                // 가장 최근 로그의 감정
                const lastLog = logs[logs.length - 1];
                timeline.push({
                    hour,
                    emotion: lastLog.emotion,
                    emoji: lastLog.emoji,
                    hue: lastLog.hue,
                    colorHex: lastLog.colorHex,
                    count: logs.length
                });
            } else {
                timeline.push({
                    hour,
                    emotion: null,
                    emoji: null,
                    hue: null,
                    colorHex: null,
                    count: 0
                });
            }
        }
        
        res.json({ timeline });
    } catch (error) {
        console.error('[ERROR] stats/timeline:', error);
        res.status(500).json({ error: error.message });
    }
});

// 전체 요약
app.get('/api/stats/summary', (req, res) => {
    try {
        const todayStart = getToday();
        const todayEmotionLogs = emotionLogs.filter(log => log.timestamp >= todayStart);
        
        // 평균 Hue 계산
        let avgHue = 120;
        if (emotionLogs.length > 0) {
            const sumHue = emotionLogs.reduce((sum, log) => sum + log.hue, 0);
            avgHue = Math.round(sumHue / emotionLogs.length);
        }
        
        // 가장 많은 감정
        const emotionCounts = {};
        emotionLogs.forEach(log => {
            emotionCounts[log.emotion] = (emotionCounts[log.emotion] || 0) + 1;
        });
        
        let topEmotion = 'calm';
        let maxCount = 0;
        for (const [emotion, count] of Object.entries(emotionCounts)) {
            if (count > maxCount) {
                maxCount = count;
                topEmotion = emotion;
            }
        }
        
        res.json({
            totalAnalyses: emotionLogs.length,
            todayAnalyses: todayEmotionLogs.length,
            topEmotion,
            topEmotionEmoji: getEmotionEmoji(topEmotion),
            topEmotionKorean: getEmotionKorean(topEmotion),
            averageHue: avgHue,
            weatherLogs: weatherLogs.length,
            stateLogs: stateLogs.length
        });
    } catch (error) {
        console.error('[ERROR] stats/summary:', error);
        res.status(500).json({ error: error.message });
    }
});

// 최근 로그
app.get('/api/stats/recent', (req, res) => {
    try {
        const limit = Math.min(parseInt(req.query.limit) || 10, 50);
        
        const recentEmotions = emotionLogs
            .slice(-limit)
            .reverse()
            .map(log => ({
                ...log,
                timeString: getKoreanTime(log.timestamp)
            }));
        
        res.json({ logs: recentEmotions });
    } catch (error) {
        console.error('[ERROR] stats/recent:', error);
        res.status(500).json({ error: error.message });
    }
});

// 현재 상태
app.get('/api/stats/current', (req, res) => {
    try {
        res.json({
            ...currentState,
            emotionEmoji: getEmotionEmoji(currentState.emotion),
            emotionKorean: getEmotionKorean(currentState.emotion),
            weatherEmoji: getWeatherEmoji(currentState.weather),
            timeString: getKoreanTime(currentState.timestamp)
        });
    } catch (error) {
        console.error('[ERROR] stats/current:', error);
        res.status(500).json({ error: error.message });
    }
});

// ==================== 기타 엔드포인트 ====================

// 서버 상태
app.get('/api/status', (req, res) => {
    res.json({
        status: 'ok',
        server: 'EmoLamp Statistics Server',
        version: '2.0.0',
        uptime: process.uptime(),
        logs: {
            emotion: emotionLogs.length,
            weather: weatherLogs.length,
            state: stateLogs.length
        },
        timestamp: Date.now()
    });
});

// 헬스체크 (Cloudtype용)
app.get('/healthz', (req, res) => {
    res.status(200).send('OK');
});

// 대시보드 HTML
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
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
    console.log(`  EmoLamp Statistics Server v2.0.0`);
    console.log(`  Running on port ${PORT}`);
    console.log(`  http://localhost:${PORT}`);
    console.log('========================================');
});

// ==================== 정리 작업 ====================
// 오래된 로그 정리 (1시간마다, 24시간 이상 된 로그)
setInterval(() => {
    const now = Date.now();
    const maxAge = 24 * 60 * 60 * 1000; // 24시간
    
    const beforeEmotion = emotionLogs.length;
    const beforeWeather = weatherLogs.length;
    const beforeState = stateLogs.length;
    
    // 필터링하여 새 배열로 교체
    const filteredEmotion = emotionLogs.filter(log => now - log.timestamp < maxAge);
    const filteredWeather = weatherLogs.filter(log => now - log.timestamp < maxAge);
    const filteredState = stateLogs.filter(log => now - log.timestamp < maxAge);
    
    emotionLogs.length = 0;
    emotionLogs.push(...filteredEmotion);
    
    weatherLogs.length = 0;
    weatherLogs.push(...filteredWeather);
    
    stateLogs.length = 0;
    stateLogs.push(...filteredState);
    
    const cleaned = (beforeEmotion - emotionLogs.length) + 
                   (beforeWeather - weatherLogs.length) + 
                   (beforeState - stateLogs.length);
    
    if (cleaned > 0) {
        console.log(`[CLEANUP] Removed ${cleaned} old logs`);
    }
}, 60 * 60 * 1000);