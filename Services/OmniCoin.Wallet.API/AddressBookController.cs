

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using EdjCase.JsonRpc.Router.Abstractions;
using OmniCoin.Business;
using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;

namespace OmniCoin.Wallet.API
{
    public class AddressBookController : BaseRpcController
    {
        public IRpcMethodResult AddNewAddressBookItem(string address, string tag)
        {
            try
            {
                var addressBookComponent = new AddressBookComponent();

                if (new AccountComponent().GetAccountById(address) != null)
                {
                    throw new CommonException(ErrorCode.Service.AddressBook.CAN_NOT_ADDED_SELF_ACCOUNT_INTO_ADDRESS_BOOK);
                }
                else
                {
                    addressBookComponent.SetTag(address?.Trim() ?? null, tag?.Trim() ?? null);
                    return Ok();
                }
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult UpsertAddrBookItem(string oldAddress, string address, string tag)
        {
            try
            {
                var addressBookComponent = new AddressBookComponent();

                if (new AccountComponent().GetAccountById(address) != null)
                {
                    throw new CommonException(ErrorCode.Service.AddressBook.CAN_NOT_ADDED_SELF_ACCOUNT_INTO_ADDRESS_BOOK);
                }
                else
                {
                    addressBookComponent.Upsert(oldAddress?.Trim(), address?.Trim() ?? null, tag?.Trim() ?? null);
                    return Ok();
                }
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetAddressBook()
        {
            try
            {
                List<AddressBookItem> result =new AddressBookComponent().GetWholeAddressBook();
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetAddressBookByTag(string tag)
        {
            try
            {
                var result = new AddressBookComponent().GetByTag(tag);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetAddressBookItemByAddress(string address)
        {
            try
            {
                var result = new AddressBookComponent().GetByAddress(address);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult DeleteAddressBookByIds(string[] addresses)
        {
            try
            {
                var addressBookComponent = new AddressBookComponent();
                addressBookComponent.Delete(addresses);
                return Ok();
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
    }
}
