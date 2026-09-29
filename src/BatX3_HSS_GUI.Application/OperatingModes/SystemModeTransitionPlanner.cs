using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Application.OperatingModes
{

    public static class SystemModeTransitionPlanner
    {
        public static IReadOnlyList<SystemOperatingMode>
            CreateTransitionPath(
                SystemOperatingMode currentMode,
                SystemOperatingMode targetMode)
        {
            if (currentMode == targetMode)
            {
                return Array.Empty<SystemOperatingMode>();
            }

            /*
             * ESTOP her çalışma modundan doğrudan girilebilir.
             */
            if (targetMode == SystemOperatingMode.EmergencyStop)
            {
                return
                [
                    SystemOperatingMode.EmergencyStop
                ];
            }

            /*
             * ESTOP'tan yalnızca IDLE'a çıkılabilir.
             *
             * ESTOP -> MANUAL/AUTO/LOOP_DEMO tek bir kullanıcı
             * işlemi olarak gerçekleştirilemez. Operatör önce
             * açıkça IDLE'a geçmelidir.
             */
            if (currentMode == SystemOperatingMode.EmergencyStop)
            {
                if (targetMode == SystemOperatingMode.Idle)
                {
                    return
                    [
                        SystemOperatingMode.Idle
                    ];
                }

                throw new InvalidOperationException(
                    "Acil Durdurma modundan yalnızca Bekleme moduna " +
                    "geçilebilir. Önce Bekleme moduna geçiniz.");
            }

            /*
             * IDLE başlangıç noktasıysa hedef çalışma moduna
             * doğrudan geçilebilir.
             */
            if (currentMode == SystemOperatingMode.Idle)
            {
                return
                [
                    targetMode
                ];
            }

            /*
             * Aktif çalışma modundan IDLE'a çıkış doğrudandır.
             */
            if (targetMode == SystemOperatingMode.Idle)
            {
                return
                [
                    SystemOperatingMode.Idle
                ];
            }

            /*
             * MANUAL/AUTO/LOOP_DEMO arasındaki geçişler
             * IDLE üzerinden gerçekleştirilir.
             */
            return
            [
                SystemOperatingMode.Idle,
                targetMode
            ];
        }
    }
}