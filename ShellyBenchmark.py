import asyncio
import gc
import logging
import statistics
import time
import json

from aiohttp.http_websocket import WSMessage, WSMsgType
from aiohttp import ClientWebSocketResponse

from aioshelly.common import ConnectionOptions
from aioshelly.rpc_device.device import RpcDevice
from aioshelly.rpc_device.wsrpc import WsRPC

from typing import cast, Literal


MESSAGES_PER_CYCLE = 20
WARMUP_CYCLES = 500
MEASURE_CYCLES = 5_000
ROUNDS = 5


class ImmediateWebSocket:
    __slots__ = ("_responses", "_queue", "_next_response", "closed", "sent")

    def __init__(self, responses: list[WSMessage]) -> None:
        self._responses = responses
        self._queue: asyncio.Queue[WSMessage] = asyncio.Queue()
        self._next_response = 0
        self.closed = False
        self.sent = 0

    async def send_frame(self, data: bytes, msg_type: WSMsgType) -> None:
        response = self._responses[self._next_response]
        self._next_response += 1
        self.sent += 1
        self._queue.put_nowait(response)

    async def receive(self) -> WSMessage:
        return await self._queue.get()

    async def close(self) -> None:
        self.closed = True


def build_responses(count: int) -> list[WSMessage]:
    return [
        WSMessage(
            WSMsgType.TEXT,
            (
                f'{{"id":{call_id},'
                f'"src":"shellypro4pm-f008d1d8b8b8",'
                f'"dst":"aios-benchmark",'
                f'"result":{{"was_on":false}}}}'
            ).encode(),
            None,
        )
        for call_id in range(1, count + 1)
    ]


async def send_cycle(device: RpcDevice) -> None:
    await device.switch_set(0, True)

    await device.boolean_set(0, True)

    await device.number_set(0, 42.5)

    await device.enum_set(0, "auto")

    await device.text_set(0, "benchmark")

    await device.cb_set(0, True)

    await device.button_trigger(0, "single_push")

    await device.cover_open(0)

    await device.cover_close(0)

    await device.cover_stop(0)

    await device.cover_set_position(0, 50, 25)

    await device.climate_set_target_temperature(0, 21.5)

    await device.climate_set_hvac_mode(0, "heat")

    await device.call_rpc("Light.Set", {"id": 0, "on": True, "brightness": 50})

    await device.call_rpc("CCT.Set", {"id": 0, "on": True, "brightness": 60, "ct": 4000})

    await device.call_rpc("RGB.Set", {"id": 0, "on": True, "brightness": 70, "rgb": [255, 64, 16]})

    await device.call_rpc("RGBW.Set", {"id": 0, "on": True, "brightness": 80, "rgb": [32, 128, 255], "white": 64})

    await device.call_rpc("RGBCCT.Set", {"id": 0, "on": True, "brightness": 90, "rgb": [8, 16, 32], "mode": "rgb"})

    await device.wall_display_set_screen(True)

    await device.media_set_volume(35)


async def benchmark_round() -> tuple[float, int]:
    total_cycles = WARMUP_CYCLES + MEASURE_CYCLES
    total_responses = total_cycles * MESSAGES_PER_CYCLE

    responses = build_responses(total_responses)

    rpc = WsRPC("127.0.0.1", lambda source, method, params: None)
    websocket = ImmediateWebSocket(responses)

    rpc._client = websocket
    rpc._rx_task = asyncio.create_task(rpc._rx_msgs())

    device = RpcDevice(None, None, ConnectionOptions("127.0.0.1"), rpc=rpc)

    for _ in range(WARMUP_CYCLES):
        await send_cycle(device)

    gc.collect()

    start = time.perf_counter_ns()

    for _ in range(MEASURE_CYCLES):
        await send_cycle(device)

    elapsed_ns = time.perf_counter_ns() - start

    measured_messages = MEASURE_CYCLES * MESSAGES_PER_CYCLE

    assert websocket.sent == total_responses

    await rpc.disconnect()

    return elapsed_ns / 1_000_000_000, measured_messages



NotifyMode = Literal[
    "mixed",
    "switch",
    "temperature",
    "voltmeter",
    "light",
    "presence",
]

RX_WARMUP_MESSAGES = 10_000
RX_MEASURE_MESSAGES = 100_000


class ImmediateReceiveWebSocket:
    """WebSocket mock optimized for receive-only benchmarks."""

    __slots__ = ("_responses", "_next_response", "_empty", "closed")

    def __init__(self, responses: list[WSMessage]) -> None:
        self._responses = responses
        self._next_response = 0
        self._empty = asyncio.Event()
        self.closed = False

    async def receive(self) -> WSMessage:
        if self._next_response < len(self._responses):
            response = self._responses[self._next_response]
            self._next_response += 1
            return response

        # All prepared messages have been consumed.
        # Block here so _rx_msgs() yields back to the benchmark task.
        await self._empty.wait()

        raise RuntimeError("unreachable")

    async def close(self) -> None:
        self.closed = True


def make_notify_status(payload: dict) -> WSMessage:
    frame = {
        "src": "shelly-benchmark-f008d1d8b8b8",
        "dst": "aios-benchmark",
        "method": "NotifyStatus",
        "params": payload,
    }

    return WSMessage(
        WSMsgType.TEXT,
        json.dumps(frame, separators=(",", ":")).encode(),
        None,
    )


def build_notify_status_templates(
    mode: NotifyMode = "mixed",
) -> list[WSMessage]:
    """Build a small set of realistic NotifyStatus frames.

    Two variants per component are used so that the status really changes.
    """

    payloads: dict[str, list[dict]] = {
        "switch": [
            {
                "ts": 1.0,
                "switch:0": {
                    "id": 0,
                    "source": "WS_in",
                    "output": True,
                    "apower": 8.9,
                    "voltage": 237.5,
                    "temperature": {
                        "tC": 23.5,
                        "tF": 74.3,
                    },
                },
            },
            {
                "ts": 2.0,
                "switch:0": {
                    "id": 0,
                    "source": "WS_in",
                    "output": False,
                    "apower": 0.0,
                    "voltage": 237.2,
                    "temperature": {
                        "tC": 23.6,
                        "tF": 74.5,
                    },
                },
            },
        ],
        "temperature": [
            {
                "ts": 3.0,
                "temperature:0": {
                    "id": 0,
                    "tC": 21.5,
                    "tF": 70.7,
                },
            },
            {
                "ts": 4.0,
                "temperature:0": {
                    "id": 0,
                    "tC": 21.6,
                    "tF": 70.9,
                },
            },
        ],
        "voltmeter": [
            {
                "ts": 5.0,
                "voltmeter:0": {
                    "id": 0,
                    "voltage": 12.31,
                },
            },
            {
                "ts": 6.0,
                "voltmeter:0": {
                    "id": 0,
                    "voltage": 12.42,
                },
            },
        ],
        "light": [
            {
                "ts": 7.0,
                "light:0": {
                    "id": 0,
                    "source": "WS_in",
                    "output": True,
                    "brightness": 70,
                },
            },
            {
                "ts": 8.0,
                "light:0": {
                    "id": 0,
                    "source": "WS_in",
                    "output": False,
                    "brightness": 25,
                },
            },
        ],
        # Presence detection itself is represented by PresenceZone.
        "presence": [
            {
                "ts": 9.0,
                "presencezone:0": {
                    "id": 0,
                    "value": True,
                    "num_objects": 2,
                },
            },
            {
                "ts": 10.0,
                "presencezone:0": {
                    "id": 0,
                    "value": False,
                    "num_objects": 0,
                },
            },
        ],
    }

    if mode == "mixed":
        selected = [
            payload
            for component_payloads in payloads.values()
            for payload in component_payloads
        ]
    else:
        selected = payloads[mode]

    return [make_notify_status(payload) for payload in selected]


def initial_device_status() -> dict:
    """Initial snapshot which NotifyStatus deltas will update."""

    return {
        "switch:0": {
            "id": 0,
            "source": "init",
            "output": False,
            "apower": 0.0,
            "voltage": 237.0,
            "temperature": {
                "tC": 23.0,
                "tF": 73.4,
            },
        },
        "temperature:0": {
            "id": 0,
            "tC": 21.0,
            "tF": 69.8,
        },
        "voltmeter:0": {
            "id": 0,
            "voltage": 12.0,
        },
        "light:0": {
            "id": 0,
            "source": "init",
            "output": False,
            "brightness": 0,
        },
        "presencezone:0": {
            "id": 0,
            "value": False,
            "num_objects": 0,
        },
    }


async def benchmark_notify_status_round(mode: NotifyMode = "mixed", ) -> tuple[float, int, float]:
    total_messages = RX_WARMUP_MESSAGES + RX_MEASURE_MESSAGES

    templates = build_notify_status_templates(mode)

    # Reuse WSMessage templates. orjson still parses the bytes on every
    # receive; we simply avoid allocating 110k independent byte strings.
    responses = [templates[i % len(templates)] for i in range(total_messages)]

    average_message_size = (sum(len(message.data) for message in templates) / len(templates)
    )

    # Let RpcDevice create WsRPC itself. This is important because WsRPC
    # will then have device._on_notification as its notification callback.
    device = RpcDevice(None, None, ConnectionOptions("127.0.0.1"),)

    rpc = cast(WsRPC, device._rpc)

    # Simulate an already initialized device.
    device._status = initial_device_status()
    device.initialized = True

    loop = asyncio.get_running_loop()
    done: asyncio.Future[None] = loop.create_future()

    processed = 0
    start_ns = 0
    end_ns = 0

    def on_update(_device: RpcDevice, _update_type: object,) -> None:
        nonlocal processed, start_ns, end_ns

        processed += 1

        # Warmup has just completed.
        if processed == RX_WARMUP_MESSAGES:
            start_ns = time.perf_counter_ns()

        # Last measured notification has been fully processed,
        # including RpcDevice._on_notification() and mergedicts().
        elif processed == total_messages:
            end_ns = time.perf_counter_ns()
            done.set_result(None)

    device.subscribe_updates(on_update)

    websocket = ImmediateReceiveWebSocket(responses)

    rpc._client = cast(ClientWebSocketResponse, websocket)
    rpc._rx_task = asyncio.create_task(rpc._rx_msgs())

    gc.collect()

    await done

    # At this point _rx_msgs() has consumed everything and is blocked
    # waiting for another message.
    assert processed == total_messages
    assert websocket._next_response == total_messages

    elapsed_ns = end_ns - start_ns

    await rpc.disconnect()

    return (
        elapsed_ns / 1_000_000_000,
        RX_MEASURE_MESSAGES,
        average_message_size,
    )    

async def run_notify_status_benchmark() -> None:
    modes: tuple[NotifyMode, ...] = (
        "mixed",
        "switch",
        "temperature",
        "voltmeter",
        "light",
        "presence",
    )

    for mode in modes:
        rates: list[float] = []

        print(f"\nNotifyStatus RX: {mode}")

        for round_number in range(1, ROUNDS + 1):
            elapsed, messages, average_size = (
                await benchmark_notify_status_round(mode)
            )

            messages_per_second = messages / elapsed
            microseconds_per_message = elapsed * 1_000_000 / messages
            mib_per_second = (
                messages_per_second
                * average_size
                / (1024 * 1024)
            )

            rates.append(messages_per_second)

            print(
                f"round {round_number}: "
                f"{messages_per_second:,.0f} msg/s, "
                f"{microseconds_per_message:.3f} us/msg, "
                f"{mib_per_second:.1f} MiB/s, "
                f"{average_size:.0f} B/msg"
            )

        print(f"median: {statistics.median(rates):,.0f} msg/s")
        
        
async def main() -> None:
    logging.getLogger("aioshelly").setLevel(logging.WARNING)

    rates: list[float] = []

    for round_number in range(1, ROUNDS + 1):
        elapsed, messages = await benchmark_round()

        messages_per_second = messages / elapsed
        microseconds_per_message = elapsed * 1_000_000 / messages

        rates.append(messages_per_second)

        print(f"round {round_number}: {messages_per_second:,.0f} msg/s, {microseconds_per_message:.3f} us/msg")

    print()
    print(f"median: {statistics.median(rates):,.0f} msg/s")
    
    logging.getLogger("aioshelly").setLevel(logging.WARNING)

    await run_notify_status_benchmark()


asyncio.run(main())