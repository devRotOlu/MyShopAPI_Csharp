using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MyShopAPI.Core.EntityDTO.CartDTO;
using MyShopAPI.Core.IRepository;
using MyShopAPI.Data.Entities;

namespace MyShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CartController(IMapper mapper,IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddToCart([FromBody] AddCartDTO item)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _unitOfWork.Products.Get(product => product.Id == item.ProductId && product.Quantity > 0);

            if (result == null) return BadRequest();

            var cartItem = _mapper.Map<Cart>(item);

            await _unitOfWork.Carts.Insert(cartItem);

            await _unitOfWork.Save();

            return Created();
        }
    }
}
