using System;
using System.Diagnostics;
using System.Windows.Input;

namespace OpenCvWpfTracking.Common
{
    public class RelayCommand : ICommand
    {
        private readonly Action execute;

        private readonly Func<bool> canExecute;

        /*
      * @brief  ParameterRelayCommand 생성자 함수.
      * @param Action<object> execute : 실행 이벤트 가 들어왔을때 동작하기 위한 Action함수를 등록
      * @return
      * @exception
      */
        /// <summary>
        /// 실행 동작과 실행 가능 여부 판정 콜백을 보관해 WPF 버튼 명령으로 연결한다.
        /// </summary>
        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        /*
         * @brief ParameterRelayCommand 생성자 함수.
         * @param   Action<object> execute : 실행 이벤트 가 들어왔을때 동작하기 위한 Action함수를 등록,
         * Predicate<object> canExecute : Control이 실행 가능 한지 불가능한지 판단하는 함수 등록.
         * @return
         * @exception
         */
        /// <summary>
        /// 실행 동작과 실행 가능 여부 판정 콜백을 보관해 WPF 버튼 명령으로 연결한다.
        /// </summary>
        public RelayCommand(Action executeAction, Func<bool> canExecuteFunc)
        {
            execute = executeAction ?? throw new ArgumentNullException("execute");

            canExecute = canExecuteFunc;
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

        /// <summary>
        /// 등록된 실행 가능 여부 콜백을 평가하고, 콜백이 없으면 명령 실행을 허용한다.
        /// </summary>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute();
        }

        /*
        * @brief 현재 컨트롤이 실행 가능한지 불가능한지 확인 하는 함수
        * @param   object parameter
        * @return  true : 실행가능 , false : 실행 불가능
        * @exception
        */
        /// <summary>
        /// 버튼에 연결된 실행 콜백을 호출한다.
        /// </summary>
        public void Execute(object parameter)
        {
            execute();
        }

        /*
         * @brief CanExecuteChaged 이벤트를 발생시킵니다
         * @param
         * @return
         * @exception
         */
        /// <summary>
        /// 명령 실행 가능 여부가 바뀌었음을 WPF 명령 갱신 경로에 알린다.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /*
         * @brief CanExecuteChaged 이벤트를 발생시킵니다
         * @param
         * @return
         * @exception
         */
        /// <summary>
        /// WPF에 명령 실행 가능 여부의 재평가를 요청한다.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
