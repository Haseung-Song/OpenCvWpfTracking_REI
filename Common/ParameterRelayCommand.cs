using System;
using System.Diagnostics;
using System.Windows.Input;

namespace OpenCvWpfTracking.Common
{
    /*
    *  @class  ParameterRelayCommand
    *  @brief  ICommand를 상속 받아  UI에서 이벤트 받아 처리하는 함수 구현
    *  @warning
    */
    public class ParameterRelayCommand : ICommand
    {
        private readonly Action<object> execute;

        private readonly Predicate<object> canExecute;

        /*
       * @brief  ParameterRelayCommand 생성자 함수.
       * @param Action<object> execute : 실행 이벤트 가 들어왔을때 동작하기 위한 Action함수를 등록
       * @return
       * @exception
       */
        /// <summary>
        /// 버튼 매개변수를 받을 실행 동작과 실행 가능 여부 판정 콜백을 연결한다.
        /// </summary>
        public ParameterRelayCommand(Action<object> execute) : this(execute, null)
        {
        }

        /*
         * @brief ParameterRelayCommand 생성자 함수.
         * @param   Action<object> execute : 실행 이벤트 가 들어왔을때 동작하기 위한 Action함수를 등록, Predicate<object> canExecute : Control이 실행 가능 한지 불가능한지 판단하는 함수 등록
         * @return
         * @exception
         */
        /// <summary>
        /// 버튼 매개변수를 받을 실행 동작과 실행 가능 여부 판정 콜백을 연결한다.
        /// </summary>
        public ParameterRelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException("execute");

            this.canExecute = canExecute;
        }

        /*
       * @brief 현재 컨트롤이 실행 가능한지 불가능한지 확인 하는 함수
       * @param   object parameter
       * @return  true : 실행가능 , false : 실행 불가능
       * @exception
       */
        /// <summary>
        /// 등록된 실행 가능 여부 콜백을 평가하고, 콜백이 없으면 명령 실행을 허용한다.
        /// </summary>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
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
            execute(parameter);
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
