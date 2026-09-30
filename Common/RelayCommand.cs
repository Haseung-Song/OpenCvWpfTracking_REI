using System;
using System.Diagnostics;
using System.Windows.Input;

namespace OpenCvWpfTracking.Common
{
    public class RelayCommand : ICommand
    {
        private readonly Action execute;

        private readonly Func<bool> canExecute;

        /**
      * @brief  ParameterRelayCommand 생성자 함수.
      * @param Action<object> execute : 실행 이벤트 가 들어왔을때 동작하기 위한 Action함수를 등록
      * @return
      * @exception
      */
        /// <summary>
        /// RelayCommand 동작 수행 함수.
        /// </summary>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        /**
         * @brief ParameterRelayCommand 생성자 함수.
         * @param   Action<object> execute : 실행 이벤트 가 들어왔을때 동작하기 위한 Action함수를 등록, 
         * Predicate<object> canExecute : Control이 실행 가능 한지 불가능한지 판단하는 함수 등록.
         * @return
         * @exception
         */
        /// <summary>
        /// RelayCommand 동작 수행 함수.
        /// </summary>
        public RelayCommand(Action executeAction, Func<bool> canExecuteFunc)
        {
            execute = executeAction ?? throw new ArgumentNullException("execute");
            canExecute = canExecuteFunc;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// CanExecute 상태 확인 함수.
        /// </summary>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute();
        }

        /**
        * @brief 현재 컨트롤이 실행 가능한지 불가능한지 확인 하는 함수
        * @param   object parameter
        * @return  true : 실행가능 , false : 실행 불가능
        * @exception
        */
        /// <summary>
        /// Execute 처리 함수.
        /// </summary>
        public void Execute(object parameter)
        {
            execute();
        }

        /**
         * @brief CanExecuteChaged 이벤트를 발생시킵니다
         * @param   
         * @return  
         * @exception
         */
        /// <summary>
        /// RaiseCanExecuteChanged 동작 수행 함수.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /**
         * @brief CanExecuteChaged 이벤트를 발생시킵니다
         * @param   
         * @return  
         * @exception
         */
        /// <summary>
        /// OnCanExecuteChanged 상태 및 이벤트 처리 함수.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

    }

}
