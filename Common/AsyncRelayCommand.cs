using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenCvWpfTracking.Common
{
    public class AsyncRelayCommand : ICommand
    {
        // [비동기 실행] 메서드
        private readonly Func<Task> executeAsync;

        private readonly Func<bool> canExecute;

        ///
        /// @brief AsyncRelayCommand 생성자 함수.
        /// @param Func<Task> executeAsync : 실행 이벤트가 들어왔을 때 동작하기 위한 비동기 메서드 등록
         //////
        /// <summary>
        /// 비동기 실행 동작과 실행 가능 여부 판정 콜백을 연결한다.
        /// </summary>
        public AsyncRelayCommand(Func<Task> executeAsync) : this(executeAsync, null)
        {
        }

        ///
        /// @brief AsyncRelayCommand 생성자 함수.
        /// @param Func<Task> executeAsync : 실행 이벤트가 들어왔을 때 동작하기 위한 비동기 메서드 등록,
        /// Func<bool> canExecute : Control이 실행 가능 여부를 판단하는 함수 등록
         //////
        /// <summary>
        /// 비동기 실행 동작과 실행 가능 여부 판정 콜백을 연결한다.
        /// </summary>
        public AsyncRelayCommand(Func<Task> executeAsync, Func<bool> canExecute)
        {
            this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));

            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        ///
        /// @brief 현재 컨트롤이 실행 가능한지 불가능한지 확인하는 함수
        /// @param object parameter
        /// @return true: 실행 가능, false: 실행 불가능
         //////
        /// <summary>
        /// 등록된 실행 가능 여부 콜백을 평가하고, 콜백이 없으면 명령 실행을 허용한다.
        /// </summary>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute();
        }

        ///
        /// @brief [비동기 메서드] 실행
        /// @param object parameter
         //////
        /// <summary>
        /// 비동기 명령을 실행하고 취소·예외가 UI 명령 경계를 벗어나지 않도록 처리한다.
        /// </summary>
        public async void Execute(object parameter)
        {
            try
            {
                await ExecuteAsync();
            }
            catch (OperationCanceledException)
            {
                // 2026-09-17: STOP에 의한 정상 취소가 UI 스레드까지 전파되어
                // 프로그램을 종료하지 않도록 최종 명령 경계에서 흡수한다.
                Debug.WriteLine("[ASYNC COMMAND] Operation canceled by user.");
            }
            catch (Exception exception)
            {
                Debug.WriteLine("[ASYNC COMMAND ERROR] " + exception);
            }
        }

        ///
        /// @brief 비동기 메서드를 직접 호출
        /// @return Task
         //////
        /// <summary>
        /// 등록된 비동기 실행 콜백의 완료를 기다린다.
        /// </summary>
        public async Task ExecuteAsync()
        {
            await executeAsync();
        }

        ///
        /// @brief CanExecuteChanged 이벤트를 발생시킵니다
         //////
        /// <summary>
        /// 명령 실행 가능 여부가 바뀌었음을 WPF 명령 갱신 경로에 알린다.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        ///
        /// @brief CanExecuteChanged 이벤트를 발생시킵니다
         //////
        /// <summary>
        /// WPF에 명령 실행 가능 여부의 재평가를 요청한다.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
