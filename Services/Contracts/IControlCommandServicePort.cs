using System;

namespace OpenCvWpfTracking.Services.Communication
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IControlCommandServicePort
    {
        bool UseUnsignedWebAgentPanCoordinates { get; set; }

        bool UseUnsignedWebAgentTiltCoordinates { get; set; }

        bool SendPrecisionAbsolute(bool tilt, double guiAngle, double speed, double maximum);

        bool SendPrecisionJog(byte direction, double panSpeed, double tiltSpeed, double maximum);

        bool SendAutoFocusCommand(byte command, byte target, byte value);

        bool SendCommand(byte cmd1, byte cmd2, byte data1, byte data2);

        bool SetWebAgentTelemetryRate(byte category, byte frequencyHz);

        bool RequestWebAgentTelemetrySetting(byte category);

        bool RequestWebAgentTelemetrySnapshot(byte category);

        bool RequestGpsTelemetry();

        bool SetGpsAutoTxRate(byte frequencyHz);

        bool RequestGpsAutoTxRate();

        bool SetDevicePower(byte deviceCode, bool turnOn);

        bool SetRpyAutoTxRate(byte frequencyHz);

        bool SetRpyOffsets(short rollHundredths, short pitchHundredths, ushort yawHundredths);

        bool RequestStoredRpyOffsets();

        bool RequestStoredRpyAutoTxRate();

        bool RequestSystemDateTime();

        bool SetSystemDateTimeUtc(DateTime utcDateTime);

        bool RequestWebAgentIdentity();

        bool RequestLensCapabilityAndTelemetry(byte target);

        bool RequestImuTelemetry();

        bool RequestCameraFeatureCapability(byte target);

        bool RequestCameraFeatureState(byte target, byte featureId);

        bool SetCameraFeature(byte featureId, byte target, byte value);

        bool RequestWebAgentPositionZero(byte axis);

        bool RequestWebAgentTiltLimits();

        bool SetPanViaZeroMode();

        bool SetPanShortestPathMode();

        bool SetLaPresetId(ushort presetId);

        bool SetLaPresetPan(double pan);

        bool SetLaPresetTilt(double tilt);

        bool SetLaPresetZoom(ushort position);

        bool SetLaPresetFocusAndCommit(ushort position);

        bool ClearAllLaPresets();

        bool AddPresetPoint(byte presetNumber);

        bool RemovePresetPoint(byte presetNumber);

        bool MoveToPresetPoint(byte presetNumber);

        bool StopPresetScan();

        bool SetPanPositionSpeed(double speedDegreesPerSecond);

        bool SetTiltPositionSpeed(double speedDegreesPerSecond);

        bool PanGoPosition(double pan);

        bool StartPanRight(byte speed = 20);

        bool StartPanLeft(byte speed = 20);

        bool TiltGoPosition(double tilt);

        bool StartTiltUp(byte speed = 20);

        bool StartTiltDown(byte speed = 20);

        bool StartPanLeftTiltUp(byte panSpeed = 20, byte tiltSpeed = 20);

        bool StartPanRightTiltUp(byte panSpeed = 20, byte tiltSpeed = 20);

        bool StartPanLeftTiltDown(byte panSpeed = 20, byte tiltSpeed = 20);

        bool StartPanRightTiltDown(byte panSpeed = 20, byte tiltSpeed = 20);

        bool StopPanPositionMove();

        bool StopTiltPositionMove();

        bool StopPanTiltPositionMove();

        bool StopMove();

        bool EoZoomGoPosition(short zoom);

        bool StartEoZoomTele();

        bool StartEoZoomWide();

        bool StopEoZoom();

        bool EoFocusGoPosition(short focus);

        bool StartEoFocusNear();

        bool StartEoFocusFar();

        bool StopEoFocus();

        bool StartEoAutoFocus();

        bool LaIrZoomGoPosition(int standardPosition);

        bool IrZoomGoPosition(short zoom);

        bool StartIrZoomTele();

        bool StartIrZoomWide();

        bool StopIrZoom();

        /// <summary>LA 수신 Raw 좌표를 그대로 사용하는 0x2B 절대 포커스 명령.</summary>
        bool LaIrFocusGoPosition(int rawPosition);

        bool IrFocusGoPosition(short focus);

        bool StartIrFocusNear();

        bool StartIrFocusFar();

        bool StopIrFocus();

        bool StartIrDigitalZoomIn();

        bool StartIrDigitalZoomOut();

        bool StopIrDigitalZoom();

        bool StartIrAutoFocus();

        bool SelectNextIrPalette();

        bool SelectPreviousIrPalette();

        bool SelectIrBlackHot();

        bool SelectIrWhiteHot();

        bool SelectIrRainbow();

        bool RequestIrNuc();
    }
}
