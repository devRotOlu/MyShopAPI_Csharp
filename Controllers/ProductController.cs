using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShopAPI.Core.EntityDTO.ProductDTO;
using MyShopAPI.Core.IRepository;
using MyShopAPI.Data.Entities;
using MyShopAPI.Services.Image;

namespace MyShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _unitOfWork;

        public ProductController(IMapper mapper, IPhotoService photoService, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _photoService = photoService;
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> AddProduct([FromForm] AddProductDTO productDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = _mapper.Map<Product>(productDTO);

            product.Images = new List<Image>();

            foreach (var file in productDTO.Photos)
            {
                var photo = await _photoService.AddPhotoAsync(file);

                if (photo.Error != null) return BadRequest(photo.Error.Message);

                product.Images.Add(new Image
                {
                    Url = photo.SecureUrl.AbsoluteUri,
                    PublicId = photo.PublicId,
                });
            }

            await _unitOfWork.Products.Insert(product);

            await _unitOfWork.Save();

            return Created();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProducts()
        {
            var results = await _unitOfWork.Products.GetAll(product => product.Quantity != 0, include: product => product.Include(product => product.Images));

            if (results == null)
            {
                return NoContent();
            }

            var products = _mapper.Map<IEnumerable<GetProductDTO>>(results);
            return Ok(products);
        }
    }
}
